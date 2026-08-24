using Microsoft.EntityFrameworkCore;
using TaskManager.Business.UnitOfWork;
using TaskManager.Bussiness.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;
using TaskManager.API.DTOs.Workspace;
using TaskManager.API.Exceptions;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Bussiness.Authorization;
using TaskManager.Data.Entities;
using TaskManager.Data.Enums;

namespace TaskManager.API.Services
{
    public class WorkspaceService : IWorkspaceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<WorkspaceService> _logger;
        private readonly IWorkspaceAuthorizationService _authService;

        public WorkspaceService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            IAuditLogService auditLogService,
            ILogger<WorkspaceService> logger,
            IWorkspaceAuthorizationService authService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _auditLogService = auditLogService;
            _logger = logger;
            _authService = authService;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static string ToSlug(string name)
        {
            var slug = Regex.Replace(name.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-");
            slug = slug.Trim('-');
            return string.IsNullOrEmpty(slug) ? "workspace" : slug;
        }

        private async Task<string> GetUniqueSlugAsync(string baseSlug, CancellationToken cancellationToken)
        {
            var slug = baseSlug;
            var suffix = 1;
            while (await _unitOfWork.Workspaces.GetAllQuery()
                .AnyAsync(w => w.Slug == slug, cancellationToken))
            {
                slug = $"{baseSlug}-{suffix++}";
            }
            return slug;
        }

        // PIPELINE HELPER: resolves the caller's member (Visibility stage),
        // then throws NotFoundException / ForbiddenException according to the
        // pipeline result. Returns the member for BR guards in the caller.
        private async Task<WorkspaceMember> RunPipelineAsync(
            long workspaceId,
            string currentUserId,
            string permission,
            CancellationToken cancellationToken)
        {
            var authResult = await _authService.AuthorizeAsync(workspaceId, currentUserId, permission);

            if (!authResult.Succeeded)
            {
                _logger.LogWarning("Workspace lifecycle pipeline failed. Reason: {Reason}, UserId: {UserId}, WorkspaceId: {WorkspaceId}",
                    authResult.FailureReason, currentUserId, workspaceId);

                throw authResult.FailureReason == AuthorizationFailureReason.NotFound
                    ? new NotFoundException(authResult.Message)
                    : new ForbiddenException(authResult.Message);
            }

            // Visibility already confirmed the member is active (S-8) and the
            // workspace exists; re-resolve for BR guards.
            var member = await _unitOfWork.WorkspaceMembers.GetAllQuery()
                .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == currentUserId, cancellationToken);

            return member ?? throw new NotFoundException("Workspace membership not found.");
        }

        private async Task<Workspace> GetWorkspaceAsync(long workspaceId, CancellationToken cancellationToken)
        {
            return await _unitOfWork.Workspaces.GetByIdAsync(workspaceId, cancellationToken)
                ?? throw new NotFoundException("Workspace not found.");
        }

        // ── Create ───────────────────────────────────────────────────────────

        public async Task<long> CreateWorkspaceAsync(WorkspaceCreateDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(currentUserId);
            if (user is null)
                throw new BadRequestException("Current user could not be resolved.");

            var slug = await GetUniqueSlugAsync(ToSlug(dto.Name), cancellationToken);

            var workspace = new Workspace
            {
                Name = dto.Name.Trim(),
                Slug = slug,
                Description = dto.Description?.Trim(),
                Status = WorkspaceStatus.Active
            };

            await _unitOfWork.Workspaces.AddAsync(workspace, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            // The creator automatically becomes the workspace Owner — no one else
            // can ever seed this relationship.
            var ownerMember = new WorkspaceMember
            {
                WorkspaceId = workspace.Id,
                UserId = currentUserId,
                Role = WorkspaceRole.Owner,
                Status = WorkspaceMemberStatus.Active
            };

            await _unitOfWork.WorkspaceMembers.AddAsync(ownerMember, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            var newValues = JsonSerializer.Serialize(new { Name = workspace.Name, Slug = workspace.Slug, OwnerUserId = currentUserId });
            await _auditLogService.LogAsync(currentUserId, "Create Workspace", nameof(Workspace), workspace.Id.ToString(), workspaceId: workspace.Id, oldValues: null, newValues: newValues, cancellationToken: cancellationToken);
            await _auditLogService.LogAsync(currentUserId, "Join Workspace as Owner", nameof(WorkspaceMember), ownerMember.Id.ToString(), workspaceId: workspace.Id, oldValues: null, newValues: JsonSerializer.Serialize(new { WorkspaceId = workspace.Id, Role = WorkspaceRole.Owner }), cancellationToken: cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Workspace created. Id: {Id}, Name: {Name}, OwnerUserId: {UserId}", workspace.Id, workspace.Name, currentUserId);

            return workspace.Id;
        }

        // ── List (own memberships) ───────────────────────────────────────────

        public async Task<IEnumerable<WorkspaceReadDto>> GetMyWorkspacesAsync(string currentUserId, CancellationToken cancellationToken = default)
        {
            var memberships = await _unitOfWork.WorkspaceMembers.GetAllQuery()
                .AsNoTracking()
                .Where(wm => wm.UserId == currentUserId)
                .Select(wm => new
                {
                    wm.Id,
                    Workspace = wm.Workspace,
                    wm.Role
                })
                .ToListAsync(cancellationToken);

            return memberships.Select(m => new WorkspaceReadDto
            {
                Id = m.Workspace.Id,
                Name = m.Workspace.Name,
                Slug = m.Workspace.Slug,
                Description = m.Workspace.Description,
                LogoUrl = m.Workspace.LogoUrl,
                Status = m.Workspace.Status,
                Role = m.Role
            });
        }

        // ── Lifecycle: Suspend / Activate ────────────────────────────────────

        // PIPELINE: Visibility -> Permission (Workspace.Update) -> BR-WS-02
        // (workspace must be Active to suspend; suspending an already-suspended
        // workspace is rejected).
        public async Task SuspendWorkspaceAsync(long workspaceId, string currentUserId, CancellationToken cancellationToken = default)
        {
            var workspace = await GetWorkspaceAsync(workspaceId, cancellationToken);

            var caller = await RunPipelineAsync(workspaceId, currentUserId, Permissions.WorkspaceUpdate, cancellationToken);

            if (workspace.Status != WorkspaceStatus.Active)
            {
                _logger.LogWarning("Cannot suspend workspace. Status: {Status}, WorkspaceId: {WorkspaceId}", workspace.Status, workspaceId);
                throw new BadRequestException("Only an active workspace can be suspended.");
            }

            // BR-WS-02: the Owner cannot be suspended by anyone, but the Owner
            // suspends the workspace itself — that is allowed. The suspension
            // affects the workspace, not the caller.

            workspace.Status = WorkspaceStatus.Suspended;
            workspace.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Workspaces.Update(workspace);

            // Suspending the workspace suspends all active members (workspace
            // suspension cascades to membership).
            var activeMembers = await _unitOfWork.WorkspaceMembers.GetAllQuery()
                .Where(m => m.WorkspaceId == workspaceId && m.Status == WorkspaceMemberStatus.Active)
                .ToListAsync(cancellationToken);

            foreach (var m in activeMembers)
            {
                m.Status = WorkspaceMemberStatus.Suspended;
                m.UpdatedAt = DateTime.UtcNow;
            }

            await _auditLogService.LogAsync(currentUserId, "Suspend Workspace", nameof(Workspace), workspaceId.ToString(),
                workspaceId: workspaceId, oldValues: WorkspaceStatus.Active.ToString(), newValues: WorkspaceStatus.Suspended.ToString(), cancellationToken: cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Workspace suspended. WorkspaceId: {WorkspaceId}, By: {UserId}", workspaceId, currentUserId);
        }

        // PIPELINE: Visibility -> Permission (Workspace.Update) -> BR-WS-02
        // (workspace must be Suspended to activate).
        public async Task ActivateWorkspaceAsync(long workspaceId, string currentUserId, CancellationToken cancellationToken = default)
        {
            var workspace = await GetWorkspaceAsync(workspaceId, cancellationToken);

            await RunPipelineAsync(workspaceId, currentUserId, Permissions.WorkspaceUpdate, cancellationToken);

            if (workspace.Status != WorkspaceStatus.Suspended)
            {
                _logger.LogWarning("Cannot activate workspace. Status: {Status}, WorkspaceId: {WorkspaceId}", workspace.Status, workspaceId);
                throw new BadRequestException("Only a suspended workspace can be activated.");
            }

            workspace.Status = WorkspaceStatus.Active;
            workspace.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Workspaces.Update(workspace);

            // Activating the workspace reactivates non-Owner suspended members.
            // The Owner stays in their role but is also reactivated here.
            var suspendedMembers = await _unitOfWork.WorkspaceMembers.GetAllQuery()
                .Where(m => m.WorkspaceId == workspaceId && m.Status == WorkspaceMemberStatus.Suspended)
                .ToListAsync(cancellationToken);

            foreach (var m in suspendedMembers)
            {
                m.Status = WorkspaceMemberStatus.Active;
                m.UpdatedAt = DateTime.UtcNow;
            }

            await _auditLogService.LogAsync(currentUserId, "Activate Workspace", nameof(Workspace), workspaceId.ToString(),
                workspaceId: workspaceId, oldValues: WorkspaceStatus.Suspended.ToString(), newValues: WorkspaceStatus.Active.ToString(), cancellationToken: cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Workspace activated. WorkspaceId: {WorkspaceId}, By: {UserId}", workspaceId, currentUserId);
        }

        // ── Lifecycle: Archive / Restore ─────────────────────────────────────

        // PIPELINE: Visibility -> Permission (Workspace.Archive) -> BR-WS-03
        // (only an Active workspace can be archived).
        public async Task ArchiveWorkspaceAsync(long workspaceId, string currentUserId, CancellationToken cancellationToken = default)
        {
            var workspace = await GetWorkspaceAsync(workspaceId, cancellationToken);

            var caller = await RunPipelineAsync(workspaceId, currentUserId, Permissions.WorkspaceArchive, cancellationToken);

            if (workspace.Status != WorkspaceStatus.Active)
            {
                _logger.LogWarning("Cannot archive workspace. Status: {Status}, WorkspaceId: {WorkspaceId}", workspace.Status, workspaceId);
                throw new BadRequestException("Only an active workspace can be archived.");
            }

            // BR-WS-03: only the Owner may archive the workspace.
            if (caller.Role != WorkspaceRole.Owner)
            {
                _logger.LogWarning("Archive requires Owner role. Role: {Role}, WorkspaceId: {WorkspaceId}", caller.Role, workspaceId);
                throw new ForbiddenException("Only the workspace Owner can archive it.");
            }

            workspace.Status = WorkspaceStatus.Archived;
            workspace.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Workspaces.Update(workspace);

            await _auditLogService.LogAsync(currentUserId, "Archive Workspace", nameof(Workspace), workspaceId.ToString(),
                workspaceId: workspaceId, oldValues: WorkspaceStatus.Active.ToString(), newValues: WorkspaceStatus.Archived.ToString(), cancellationToken: cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Workspace archived. WorkspaceId: {WorkspaceId}, By: {UserId}", workspaceId, currentUserId);
        }

        // PIPELINE: Visibility -> Permission (Workspace.Update) -> BR-WS-03
        // (only an Archived workspace can be restored; Owner only).
        public async Task RestoreWorkspaceAsync(long workspaceId, string currentUserId, CancellationToken cancellationToken = default)
        {
            var workspace = await GetWorkspaceAsync(workspaceId, cancellationToken);

            var caller = await RunPipelineAsync(workspaceId, currentUserId, Permissions.WorkspaceUpdate, cancellationToken);

            if (workspace.Status != WorkspaceStatus.Archived)
            {
                _logger.LogWarning("Cannot restore workspace. Status: {Status}, WorkspaceId: {WorkspaceId}", workspace.Status, workspaceId);
                throw new BadRequestException("Only an archived workspace can be restored.");
            }

            // BR-WS-03: only the Owner may restore the workspace.
            if (caller.Role != WorkspaceRole.Owner)
            {
                _logger.LogWarning("Restore requires Owner role. Role: {Role}, WorkspaceId: {WorkspaceId}", caller.Role, workspaceId);
                throw new ForbiddenException("Only the workspace Owner can restore it.");
            }

            workspace.Status = WorkspaceStatus.Active;
            workspace.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Workspaces.Update(workspace);

            await _auditLogService.LogAsync(currentUserId, "Restore Workspace", nameof(Workspace), workspaceId.ToString(),
                workspaceId: workspaceId, oldValues: WorkspaceStatus.Archived.ToString(), newValues: WorkspaceStatus.Active.ToString(), cancellationToken: cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Workspace restored. WorkspaceId: {WorkspaceId}, By: {UserId}", workspaceId, currentUserId);
        }

        // ── Transfer Ownership ───────────────────────────────────────────────

        // PIPELINE: Visibility -> Permission (Workspace.TransferOwnership) ->
        // BR-WS-04 (target must be a different active member; caller demoted to
        // Admin, target promoted to Owner).
        public async Task TransferOwnershipAsync(long workspaceId, string targetUserId, string currentUserId, CancellationToken cancellationToken = default)
        {
            if (string.Equals(targetUserId, currentUserId, StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException("Ownership cannot be transferred to the current Owner.");
            }

            await RunPipelineAsync(workspaceId, currentUserId, Permissions.WorkspaceTransferOwnership, cancellationToken);

            var target = await _unitOfWork.WorkspaceMembers.GetAllQuery()
                .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == targetUserId, cancellationToken);

            if (target is null || target.IsDeleted || target.Status != WorkspaceMemberStatus.Active)
            {
                _logger.LogWarning("Transfer target not an active member. WorkspaceId: {WorkspaceId}, TargetUserId: {TargetUserId}", workspaceId, targetUserId);
                throw new BadRequestException("The target user is not an active member of this workspace.");
            }

            var caller = await _unitOfWork.WorkspaceMembers.GetAllQuery()
                .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == currentUserId, cancellationToken);

            if (caller is null)
                throw new NotFoundException("Caller membership not found.");

            // BR-WS-04
            var oldValues = JsonSerializer.Serialize(new { CallerRole = caller.Role.ToString(), TargetRole = target.Role.ToString() });

            caller.Role = WorkspaceRole.Admin;
            caller.UpdatedAt = DateTime.UtcNow;
            target.Role = WorkspaceRole.Owner;
            target.UpdatedAt = DateTime.UtcNow;

            await _auditLogService.LogAsync(currentUserId, "Transfer Ownership", nameof(WorkspaceMember), workspaceId.ToString(),
                workspaceId: workspaceId,
                oldValues: oldValues,
                newValues: JsonSerializer.Serialize(new { CallerRole = WorkspaceRole.Admin, TargetRole = WorkspaceRole.Owner }),
                cancellationToken: cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Ownership transferred. WorkspaceId: {WorkspaceId}, From: {From}, To: {To}", workspaceId, currentUserId, targetUserId);
        }
    }
}
