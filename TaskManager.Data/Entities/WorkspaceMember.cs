using TaskManager.Data.Enums;

namespace TaskManager.Data.Entities
{
    public class WorkspaceMember : BaseEntity
    {
        public long WorkspaceId { get; set; }
        public Workspace Workspace { get; set; } = null!;

        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        public WorkspaceRole Role { get; set; }

        public WorkspaceMemberStatus Status { get; set; } = WorkspaceMemberStatus.Active;

        public ICollection<TeamMember> TeamMemberships { get; set; }
            = new List<TeamMember>();

        public ICollection<ProjectMember> ProjectMemberships { get; set; }
            = new List<ProjectMember>();

        public ICollection<TaskAssignment> TaskAssignments { get; set; }
            = new List<TaskAssignment>();

        public ICollection<Comment> Comments { get; set; }
            = new List<Comment>();

        public ICollection<Attachment> UploadedAttachments { get; set; }
            = new List<Attachment>();

        public ICollection<TaskItemStatusHistory> StatusChanges { get; set; }
            = new List<TaskItemStatusHistory>();

        public ICollection<Notification> Notifications { get; set; }
            = new List<Notification>();

        public ICollection<AuditLog> AuditLogs { get; set; }
            = new List<AuditLog>();

        public ICollection<Invitation> SentInvitations { get; set; }
            = new List<Invitation>();
    }
}
