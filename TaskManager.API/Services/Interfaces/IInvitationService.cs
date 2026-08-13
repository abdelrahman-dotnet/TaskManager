using TaskManager.API.DTOs.Invitation;

namespace TaskManager.Business.Services.Interfaces
{
    public interface IInvitationService
    {
        // Owner/Admin (pipeline: MembersInvite)
        Task<long> SendInvitationAsync(InvitationCreateDto dto, string currentUserId, CancellationToken cancellationToken = default);

        // Owner/Admin (pipeline: InvitationsCancel) — any active Owner/Admin of the workspace
        Task RevokeInvitationAsync(long invitationId, string currentUserId, CancellationToken cancellationToken = default);

        // Owner/Admin (pipeline: InvitationsResend) — fresh invitation for a previously-removed member
        Task<long> ResendInvitationAsync(InvitationResendDto dto, string currentUserId, CancellationToken cancellationToken = default);

        // Owner/Admin (pipeline: InvitationsView)
        Task<IEnumerable<InvitationReadDto>> GetInvitationsAsync(long workspaceId, string currentUserId, CancellationToken cancellationToken = default);

        // Owner/Admin (pipeline: InvitationsView) — single invitation by id, scoped to workspaceId
        Task<InvitationReadDto> GetInvitationByIdAsync(long invitationId, long workspaceId, string currentUserId, CancellationToken cancellationToken = default);

        // Self-service (BR-INV-02 only, no pipeline)
        Task AcceptInvitationAsync(long invitationId, string currentUserId, CancellationToken cancellationToken = default);

        Task RejectInvitationAsync(long invitationId, string currentUserId, CancellationToken cancellationToken = default);
    }
}
