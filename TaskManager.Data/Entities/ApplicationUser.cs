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

        // TODO:
        // Legacy navigation kept temporarily for incremental migration.
        // Remove after Membership system is fully adopted.
        // Do NOT use this in any new Service or Feature - use TeamMemberships instead.
        public long? TeamId { get; set; }

        public Team? Team { get; set; }

        // Teams managed by this user (Team.ManagerId) - unrelated to Membership, left as-is.
        public ICollection<Team> ManagedTeams { get; set; }
            = new List<Team>();

        // NEW: every Team this user belongs to, with their Team-scoped Role in each.
        // Coexists with TeamId/Team above until the old single-team model is retired.
        public ICollection<TeamMember> TeamMemberships { get; set; }
            = new List<TeamMember>();

        // NEW: every Project this user belongs to, with their Project-scoped Role in each.
        public ICollection<ProjectMember> ProjectMemberships { get; set; }
            = new List<ProjectMember>();

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
