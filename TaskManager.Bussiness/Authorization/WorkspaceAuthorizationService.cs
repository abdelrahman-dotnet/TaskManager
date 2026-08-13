using Microsoft.EntityFrameworkCore;
using TaskManager.Business.UnitOfWork;
using TaskManager.Bussiness.Interfaces;
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

            if (member is null || member.Status != WorkspaceMemberStatus.Active)
            {
                return AuthorizationResult.NotFound();
            }

            var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId);
            if (workspace is null || workspace.IsDeleted)
            {
                return AuthorizationResult.NotFound();
            }

            // === S-8: Workspace Suspended → Read-only mode ===
            // أي Permission غير View → Forbidden (مش NotFound، لأنه شايف الـ Workspace بس read-only).
            if (workspace.Status == WorkspaceStatus.Suspended && !permission.EndsWith(".View"))
            {
                return AuthorizationResult.Forbidden(
                    "This workspace is suspended. Only read operations are allowed.");
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
    }
}
