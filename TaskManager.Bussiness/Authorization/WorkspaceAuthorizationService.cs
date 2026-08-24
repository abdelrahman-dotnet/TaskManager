using Microsoft.EntityFrameworkCore;
using TaskManager.Business.UnitOfWork;
using TaskManager.Bussiness.Interfaces;
using TaskManager.Data.Entities;
using TaskManager.Data.Enums;

namespace TaskManager.Bussiness.Authorization
{
    public class WorkspaceAuthorizationService : IWorkspaceAuthorizationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public WorkspaceAuthorizationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<AuthorizationResult> AuthorizeAsync(
            long workspaceId,
            string currentUserId,
            string permission,
            IResourceCondition? resourceCondition = null)
        {
            // === المرحلة 1: Visibility ===
            // مش عضو، أو الـ Workspace غير موجود/محذوف → 404 (إخفاء وجود الـ Resource)
            var member = await _unitOfWork.WorkspaceMembers.GetAllQuery()
                .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId
                    && m.UserId == currentUserId
                    && !m.IsDeleted);

            var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId);
            if (workspace is null || workspace.IsDeleted)
            {
                return AuthorizationResult.NotFound();
            }

            // Workspace suspension cascades the Owner's membership to Suspended.
            // The Owner must still be able to pass the existing pipeline to reactivate
            // that same workspace; all other suspended-membership requests remain hidden.
            var isSuspendedOwnerRecovery = workspace.Status == WorkspaceStatus.Suspended
                && member is not null
                && member.Status == WorkspaceMemberStatus.Suspended
                && member.Role == WorkspaceRole.Owner
                && permission == Permissions.WorkspaceUpdate;

            var isArchivedOwnerRecovery = workspace.Status == WorkspaceStatus.Archived
                && member is not null
                && member.Status == WorkspaceMemberStatus.Active
                && member.Role == WorkspaceRole.Owner
                && permission == Permissions.WorkspaceUpdate;

            if (member is null || (member.Status != WorkspaceMemberStatus.Active && !isSuspendedOwnerRecovery))
            {
                return AuthorizationResult.NotFound();
            }

            // === S-8 / BR-WS-03: Non-active workspace → Read-only mode ===
            // Non-view operations remain forbidden, except the Owner's recovery
            // authorization above; the catalog still determines the actual role permission.
            if (workspace.Status == WorkspaceStatus.Suspended
                && !permission.EndsWith(".View")
                && !isSuspendedOwnerRecovery)
            {
                return AuthorizationResult.Forbidden(
                    "This workspace is suspended. Only read operations are allowed.");
            }

            if (workspace.Status == WorkspaceStatus.Archived
                && !permission.EndsWith(".View")
                && !isArchivedOwnerRecovery)
            {
                return AuthorizationResult.Forbidden(
                    "This workspace is archived. Only read operations are allowed.");
            }

            // === المرحلة 2: Permission ===
            if (!RolePermissionCatalog.HasPermission(member.Role, permission))
            {
                return AuthorizationResult.Forbidden(
                    $"Role '{member.Role}' does not have permission '{permission}'.");
            }

            // === المرحلة 3: Resource Condition ===
            if (resourceCondition is not null)
            {
                var satisfied = await resourceCondition.IsSatisfiedAsync(member);
                if (!satisfied)
                {
                    return AuthorizationResult.Forbidden(resourceCondition.FailureMessage);
                }
            }

            return AuthorizationResult.Success();
        }

        public async Task<List<long>> GetAuthorizedWorkspaceMemberIdsAsync(
            string currentUserId,
            string permission,
            Func<WorkspaceMember, IResourceCondition?> resourceConditionFactory,
            CancellationToken cancellationToken = default)
        {
            var candidates = await (
                from member in _unitOfWork.WorkspaceMembers.GetAllQuery()
                join workspace in _unitOfWork.Workspaces.GetAllQuery()
                    on member.WorkspaceId equals workspace.Id
                where member.UserId == currentUserId
                    && !member.IsDeleted
                    && !workspace.IsDeleted
                select new { Member = member, Workspace = workspace }
            ).ToListAsync(cancellationToken);

            var authorizedMembershipIds = new List<long>();
            foreach (var candidate in candidates)
            {
                var isSuspendedOwnerRecovery = candidate.Workspace.Status == WorkspaceStatus.Suspended
                    && candidate.Member.Status == WorkspaceMemberStatus.Suspended
                    && candidate.Member.Role == WorkspaceRole.Owner
                    && permission == Permissions.WorkspaceUpdate;

                var isArchivedOwnerRecovery = candidate.Workspace.Status == WorkspaceStatus.Archived
                    && candidate.Member.Status == WorkspaceMemberStatus.Active
                    && candidate.Member.Role == WorkspaceRole.Owner
                    && permission == Permissions.WorkspaceUpdate;

                if (candidate.Member.Status != WorkspaceMemberStatus.Active && !isSuspendedOwnerRecovery)
                {
                    continue;
                }

                if (candidate.Workspace.Status == WorkspaceStatus.Suspended
                    && !permission.EndsWith(".View")
                    && !isSuspendedOwnerRecovery)
                {
                    continue;
                }

                if (candidate.Workspace.Status == WorkspaceStatus.Archived
                    && !permission.EndsWith(".View")
                    && !isArchivedOwnerRecovery)
                {
                    continue;
                }

                if (!RolePermissionCatalog.HasPermission(candidate.Member.Role, permission))
                {
                    continue;
                }

                var resourceCondition = resourceConditionFactory(candidate.Member);
                if (resourceCondition is not null
                    && !await resourceCondition.IsSatisfiedAsync(candidate.Member))
                {
                    continue;
                }

                authorizedMembershipIds.Add(candidate.Member.Id);
            }

            return authorizedMembershipIds;
        }
    }
}
