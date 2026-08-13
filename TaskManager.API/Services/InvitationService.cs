using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManager.API.DTOs.Invitation;
using TaskManager.API.Exceptions;
using TaskManager.API.Extentions;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Business.UnitOfWork;
using TaskManager.Bussiness.Authorization;
using TaskManager.Data.Entities;
using TaskManager.Data.Enums;

namespace TaskManager.API.Services
{
    public class InvitationService : IInvitationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<InvitationService> _logger;
        private readonly IWorkspaceAuthorizationService _authService;

        public InvitationService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            IAuditLogService auditLogService,
            ILogger<InvitationService> logger,
            IWorkspaceAuthorizationService authService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _auditLogService = auditLogService;
            _logger = logger;
            _authService = authService;
        }

        // ══════════════════════════════ Helpers ══════════════════════════════

        private async Task<Invitation> LoadInvitationAsync(long invitationId, CancellationToken cancellationToken)
        {
            var invitation = await _unitOfWork.Invitations.GetByIdAsync(invitationId, cancellationToken);
            if (invitation is null)
                throw new NotFoundException("Invitation not found.");
            return invitation;
        }

        private static void ThrowIfExpiredOrClosed(Invitation invitation)
        {
            if (invitation.Status != InvitationStatus.Pending)
                throw new BadRequestException("This invitation is no longer pending and cannot be processed.");

            if (DateTime.UtcNow > invitation.ExpiresAt)
                throw new BadRequestException("This invitation has expired.");
        }

        // ══════════════════════════════ Send / Resend ══════════════════════════════

        public async Task<long> SendInvitationAsync(InvitationCreateDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            return await SendInvitationInnerAsync(
                dto.WorkspaceId,
                dto.InvitedUserId,
                dto.Role,
                dto.ExpiresAt,
                currentUserId,
                isResend: false,
                cancellationToken);
        }

        public async Task<long> ResendInvitationAsync(InvitationResendDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            return await SendInvitationInnerAsync(
                dto.WorkspaceId,
                dto.InvitedUserId,
                WorkspaceRole.Member,
                dto.ExpiresAt,
                currentUserId,
                isResend: true,
                cancellationToken);
        }

        private async Task<long> SendInvitationInnerAsync(
            long workspaceId,
            string invitedUserId,
            WorkspaceRole role,
            DateTime expiresAt,
            string currentUserId,
            bool isResend,
            CancellationToken cancellationToken)
        {
            // ── Stage 1+2: pipeline (MembersInvite / InvitationsResend) ──
            var authPermission = isResend ? Permissions.InvitationsResend : Permissions.MembersInvite;
            var authResult = await _authService.AuthorizeAsync(workspaceId, currentUserId, authPermission, null);
            if (!authResult.Succeeded)
            {
                _logger.LogWarning("Invitation pipeline failed. Reason: {Reason}, WorkspaceId: {WorkspaceId}, UserId: {UserId}",
                    authResult.FailureReason, workspaceId, currentUserId);
                throw authResult.ToAuthorizationException();
            }

            var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId, cancellationToken);
            if (workspace is null)
                throw new NotFoundException("Workspace not found.");

            // ── Business rules (BR-INV-01 / D-25 / D-26 / BR-MEM-04) ──

            var invitedUser = await _userManager.FindByIdAsync(invitedUserId);
            if (invitedUser is null)
                throw new BadRequestException("The invited user does not exist.");

            if (invitedUser.Id == currentUserId)
                throw new BadRequestException("You cannot invite yourself.");

            if (role == WorkspaceRole.Owner)
                throw new BadRequestException("Inviting with the Owner role is strictly forbidden.");

            if (role != WorkspaceRole.Admin && role != WorkspaceRole.Member)
                throw new BadRequestException("Invitations can only be issued with the Administrator or Member role.");

            if (workspace.Status != WorkspaceStatus.Active)
                throw new BadRequestException("Invitations cannot be issued for an inactive workspace.");

            if (DateTime.UtcNow >= expiresAt)
                throw new BadRequestException("The invitation expiry must be in the future.");

            var existingMember = await _unitOfWork.WorkspaceMembers.GetAllQuery()
                .Where(wm => wm.WorkspaceId == workspaceId && wm.UserId == invitedUserId)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingMember is not null)
            {
                if (existingMember.Status == WorkspaceMemberStatus.Active)
                    throw new BadRequestException("This user is already an active member of the workspace and cannot be invited.");

                if (existingMember.Status == WorkspaceMemberStatus.Suspended)
                    throw new BadRequestException("This user is suspended. Reactivate them via Unsuspend instead of re-inviting.");
            }

            var pendingInvitation = await _unitOfWork.Invitations.GetAllQuery()
                .Where(i => i.WorkspaceId == workspaceId
                            && i.InvitedUserId == invitedUserId
                            && i.Status == InvitationStatus.Pending)
                .AnyAsync(cancellationToken);

            if (pendingInvitation)
                throw new ConflictException("A pending invitation already exists for this user in this workspace.");

            var inviter = await _unitOfWork.WorkspaceMembers.GetAllQuery()
                .Where(wm => wm.WorkspaceId == workspaceId && wm.UserId == currentUserId)
                .FirstAsync(cancellationToken);

            var invitation = new Invitation
            {
                WorkspaceId = workspaceId,
                InvitedUserId = invitedUserId,
                Role = role,
                InvitedByWorkspaceMemberId = inviter.Id,
                Status = InvitationStatus.Pending,
                ExpiresAt = expiresAt.ToUniversalTime()
            };

            await _unitOfWork.Invitations.AddAsync(invitation, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            await _auditLogService.LogAsync(
                currentUserId,
                isResend ? "Resend Invitation" : "Send Invitation",
                nameof(Invitation),
                invitation.Id.ToString(),
                null,
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    WorkspaceId = workspaceId,
                    InvitedUserId = invitedUserId,
                    Role = role,
                    ExpiresAt = invitation.ExpiresAt
                }),
                cancellationToken);

            _logger.LogInformation("Invitation sent. Id: {Id}, WorkspaceId: {WorkspaceId}, InvitedUserId: {UserId}, Role: {Role}",
                invitation.Id, workspaceId, invitedUserId, role);

            // TODO (deferred, schema gap): send a personal notification to the recipient.
            // NotificationService.CreateAsync resolves a recipient WorkspaceMember, but a
            // removed member has no active membership record — this is deferred with the
            // schema pass and must not block invitation creation.

            return invitation.Id;
        }

        // ══════════════════════════════ Revoke ══════════════════════════════

        public async Task RevokeInvitationAsync(long invitationId, string currentUserId, CancellationToken cancellationToken = default)
        {
            var invitation = await LoadInvitationAsync(invitationId, cancellationToken);

            var authResult = await _authService.AuthorizeAsync(
                invitation.WorkspaceId,
                currentUserId,
                Permissions.InvitationsCancel,
                null);

            if (!authResult.Succeeded)
            {
                _logger.LogWarning("Revoke pipeline failed. Reason: {Reason}, InvitationId: {InvitationId}, UserId: {UserId}",
                    authResult.FailureReason, invitationId, currentUserId);
                throw authResult.ToAuthorizationException();
            }

            if (invitation.Status != InvitationStatus.Pending)
                throw new BadRequestException("Only pending invitations can be revoked.");

            invitation.Status = InvitationStatus.Cancelled;

            await _unitOfWork.CompleteAsync(cancellationToken);

            await _auditLogService.LogAsync(
                currentUserId,
                "Revoke Invitation",
                nameof(Invitation),
                invitation.Id.ToString(),
                System.Text.Json.JsonSerializer.Serialize(new { invitation.Status }),
                System.Text.Json.JsonSerializer.Serialize(new { Status = InvitationStatus.Cancelled }),
                cancellationToken);

            _logger.LogInformation("Invitation revoked. Id: {Id}, UserId: {UserId}", invitationId, currentUserId);
        }

        // ══════════════════════════════ Accept / Reject (self-service) ══════════════════════════════

        public async Task AcceptInvitationAsync(long invitationId, string currentUserId, CancellationToken cancellationToken = default)
        {
            // BR-INV-02 only — no pipeline (the recipient may not be a member yet).
            var invitation = await LoadInvitationAsync(invitationId, cancellationToken);

            if (invitation.InvitedUserId != currentUserId)
                throw new ForbiddenException("This invitation is not addressed to you.");

            if (invitation.Status != InvitationStatus.Pending)
                throw new BadRequestException("This invitation is no longer pending and cannot be accepted.");

            if (DateTime.UtcNow > invitation.ExpiresAt)
                throw new BadRequestException("This invitation has expired.");

            var workspace = await _unitOfWork.Workspaces.GetByIdAsync(invitation.WorkspaceId, cancellationToken);
            if (workspace is null || workspace.Status != WorkspaceStatus.Active)
                throw new BadRequestException("The workspace is no longer active.");

            if (invitation.Role != WorkspaceRole.Admin && invitation.Role != WorkspaceRole.Member)
                throw new BadRequestException("This invitation carries a role that can no longer be granted.");

            var member = await _unitOfWork.WorkspaceMembers.GetAllQuery()
                .Where(wm => wm.WorkspaceId == invitation.WorkspaceId && wm.UserId == currentUserId)
                .FirstOrDefaultAsync(cancellationToken);

            var isNewMember = member is null;
            if (!isNewMember && member.Status == WorkspaceMemberStatus.Active)
                throw new ConflictException("You are already an active member of this workspace.");

            invitation.Status = InvitationStatus.Accepted;
            invitation.AcceptedAt = DateTime.UtcNow;

            if (isNewMember)
            {
                member = new WorkspaceMember
                {
                    WorkspaceId = invitation.WorkspaceId,
                    UserId = currentUserId,
                    Role = invitation.Role,
                    Status = WorkspaceMemberStatus.Active
                };
                await _unitOfWork.WorkspaceMembers.AddAsync(member, cancellationToken);
            }
            else
            {
                // BR-MEM-04 + D-26: a previously-removed (or otherwise inactive) member's
                // record is reactivated with the invited role — no new record is created.
                member.Role = invitation.Role;
                member.Status = WorkspaceMemberStatus.Active;
            }

            await _unitOfWork.CompleteAsync(cancellationToken);

            await _auditLogService.LogAsync(
                currentUserId,
                "Accept Invitation",
                nameof(Invitation),
                invitation.Id.ToString(),
                System.Text.Json.JsonSerializer.Serialize(new { invitation.Status }),
                System.Text.Json.JsonSerializer.Serialize(new { Status = InvitationStatus.Accepted, MemberStatus = WorkspaceMemberStatus.Active, Role = invitation.Role }),
                cancellationToken);

            await _auditLogService.LogAsync(
                currentUserId,
                isNewMember ? "Join Workspace as Member" : "Reactivate Workspace Membership",
                nameof(WorkspaceMember),
                member.Id.ToString(),
                null,
                System.Text.Json.JsonSerializer.Serialize(new { WorkspaceId = invitation.WorkspaceId, Role = invitation.Role, Status = WorkspaceMemberStatus.Active }),
                cancellationToken);

            _logger.LogInformation("Invitation accepted. Id: {Id}, WorkspaceId: {WorkspaceId}, UserId: {UserId}, Role: {Role}",
                invitation.Id, invitation.WorkspaceId, currentUserId, invitation.Role);
        }

        public async Task RejectInvitationAsync(long invitationId, string currentUserId, CancellationToken cancellationToken = default)
        {
            // BR-INV-02 only — no pipeline (the recipient may not be a member yet).
            var invitation = await LoadInvitationAsync(invitationId, cancellationToken);

            if (invitation.InvitedUserId != currentUserId)
                throw new ForbiddenException("This invitation is not addressed to you.");

            if (invitation.Status != InvitationStatus.Pending)
                throw new BadRequestException("This invitation is no longer pending and cannot be rejected.");

            if (DateTime.UtcNow > invitation.ExpiresAt)
                throw new BadRequestException("This invitation has expired.");

            invitation.Status = InvitationStatus.Rejected;
            invitation.RejectedAt = DateTime.UtcNow;

            await _unitOfWork.CompleteAsync(cancellationToken);

            await _auditLogService.LogAsync(
                currentUserId,
                "Reject Invitation",
                nameof(Invitation),
                invitation.Id.ToString(),
                System.Text.Json.JsonSerializer.Serialize(new { invitation.Status }),
                System.Text.Json.JsonSerializer.Serialize(new { Status = InvitationStatus.Rejected }),
                cancellationToken);

            _logger.LogInformation("Invitation rejected. Id: {Id}, WorkspaceId: {WorkspaceId}, UserId: {UserId}",
                invitation.Id, invitation.WorkspaceId, currentUserId);
        }

        // ══════════════════════════════ List ══════════════════════════════

        public async Task<InvitationReadDto> GetInvitationByIdAsync(long invitationId, long workspaceId, string currentUserId, CancellationToken cancellationToken = default)
        {
            var invitations = await GetInvitationsAsync(workspaceId, currentUserId, cancellationToken);

            var invitation = invitations.FirstOrDefault(i => i.Id == invitationId);
            if (invitation is null)
            {
                _logger.LogWarning("Invitation not found or caller lacks access. InvitationId: {Id}, WorkspaceId: {WorkspaceId}, UserId: {UserId}",
                    invitationId, workspaceId, currentUserId);
                throw new Exceptions.NotFoundException("Invitation not found.");
            }

            return invitation;
        }

        public async Task<IEnumerable<InvitationReadDto>> GetInvitationsAsync(long workspaceId, string currentUserId, CancellationToken cancellationToken = default)
        {
            var authResult = await _authService.AuthorizeAsync(workspaceId, currentUserId, Permissions.InvitationsView, null);
            if (!authResult.Succeeded)
            {
                _logger.LogWarning("List invitations pipeline failed. Reason: {Reason}, WorkspaceId: {WorkspaceId}, UserId: {UserId}",
                    authResult.FailureReason, workspaceId, currentUserId);
                throw authResult.ToAuthorizationException();
            }

            var invitations = await _unitOfWork.Invitations.GetAllQuery()
                .AsNoTracking()
                .Where(i => i.WorkspaceId == workspaceId)
                .OrderByDescending(i => i.Id)
                .Select(i => new InvitationReadDto
                {
                    Id = i.Id,
                    WorkspaceId = i.WorkspaceId,
                    WorkspaceName = i.Workspace.Name,
                    InvitedUserId = i.InvitedUserId,
                    InvitedUserName = i.InvitedUser.UserName ?? string.Empty,
                    Role = i.Role,
                    InvitedByWorkspaceMemberId = i.InvitedByWorkspaceMemberId,
                    InvitedByUserId = i.InvitedByWorkspaceMember.UserId,
                    Status = i.Status,
                    ExpiresAt = i.ExpiresAt,
                    AcceptedAt = i.AcceptedAt,
                    RejectedAt = i.RejectedAt
                })
                .ToListAsync(cancellationToken);

            return invitations;
        }
    }
}
