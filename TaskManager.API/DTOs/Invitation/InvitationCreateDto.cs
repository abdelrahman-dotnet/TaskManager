using TaskManager.Data.Enums;

namespace TaskManager.API.DTOs.Invitation
{
    public class InvitationCreateDto
    {
        public long WorkspaceId { get; set; }

        public string InvitedUserId { get; set; } = null!;

        public WorkspaceRole Role { get; set; } = WorkspaceRole.Member;

        public DateTime ExpiresAt { get; set; }
    }

    public class InvitationResendDto
    {
        public long WorkspaceId { get; set; }

        public string InvitedUserId { get; set; } = null!;

        public DateTime ExpiresAt { get; set; }
    }
}
