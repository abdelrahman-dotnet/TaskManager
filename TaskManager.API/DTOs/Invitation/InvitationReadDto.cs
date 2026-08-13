using TaskManager.Data.Enums;

namespace TaskManager.API.DTOs.Invitation
{
    public class InvitationReadDto
    {
        public long Id { get; set; }

        public long WorkspaceId { get; set; }

        public string WorkspaceName { get; set; } = null!;

        public string InvitedUserId { get; set; } = null!;

        public string InvitedUserName { get; set; } = null!;

        public WorkspaceRole Role { get; set; }

        public long InvitedByWorkspaceMemberId { get; set; }

        public string InvitedByUserId { get; set; } = null!;

        public InvitationStatus Status { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime? AcceptedAt { get; set; }

        public DateTime? RejectedAt { get; set; }
    }
}
