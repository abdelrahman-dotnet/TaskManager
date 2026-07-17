using Microsoft.AspNetCore.Identity;

namespace TaskManager.Data.Entities
{
    public class ApplicationUser : IdentityUser
    {
        //public string FirstName { get; set; } = null!;

        //public string LastName { get; set; } = null!;
        public bool IsActive { get; set; } = true;

        public bool ShouldNotify { get; set; } = true;

        public int NotifyPeriod { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public long? TeamId { get; set; }

        public Team? Team { get; set; }

        // Teams managed by this user
        public ICollection<Team> ManagedTeams { get; set; }
            = new List<Team>();

        public ICollection<Project> CreatedProjects { get; set; }
            = new List<Project>();

        public ICollection<TaskItem> CreatedTasks { get; set; }
            = new List<TaskItem>();

        public ICollection<TaskAssignment> AssignedTasks { get; set; }
            = new List<TaskAssignment>();

        public ICollection<TaskAssignment> CreatedAssignments { get; set; }
            = new List<TaskAssignment>();

        public ICollection<Comment> Comments { get; set; }
            = new List<Comment>();

        public ICollection<Attachment> UploadedAttachments { get; set; }
            = new List<Attachment>();

        public ICollection<Notification> Notifications { get; set; }
            = new List<Notification>();

        public ICollection<TaskStatusHistory> StatusChanges { get; set; }
            = new List<TaskStatusHistory>();

        public ICollection<AuditLog> AuditLogs { get; set; }
            = new List<AuditLog>();
    }
}
