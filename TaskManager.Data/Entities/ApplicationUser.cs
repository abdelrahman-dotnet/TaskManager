using Microsoft.AspNetCore.Identity;

namespace TaskManager.Data.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsActive { get; set; } = true;

        public bool ShouldNotify { get; set; } = true;

        public int NotifyPeriod { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public ICollection<WorkspaceMember> WorkspaceMemberships { get; set; }
            = new List<WorkspaceMember>();

        public ICollection<Project> CreatedProjects { get; set; }
            = new List<Project>();

        public ICollection<TaskItem> CreatedTasks { get; set; }
            = new List<TaskItem>();

        public ICollection<Invitation> ReceivedInvitations { get; set; }
            = new List<Invitation>();
    }
}
