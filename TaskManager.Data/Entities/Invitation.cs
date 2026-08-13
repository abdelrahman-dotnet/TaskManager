using TaskManager.Data.Enums;

namespace TaskManager.Data.Entities
{
    public class Invitation : BaseEntity
    {
        public long WorkspaceId { get; set; }
        public Workspace Workspace { get; set; } = null!;

        public string InvitedUserId { get; set; } = null!;
        public ApplicationUser InvitedUser { get; set; } = null!;

        public WorkspaceRole Role { get; set; }

        public long InvitedByWorkspaceMemberId { get; set; }
        public WorkspaceMember InvitedByWorkspaceMember { get; set; } = null!;

        public InvitationStatus Status { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime? AcceptedAt { get; set; }

        public DateTime? RejectedAt { get; set; }
    }
}
