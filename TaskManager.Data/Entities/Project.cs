using System.ComponentModel.DataAnnotations;

namespace TaskManager.Data.Entities
{
    public class Project : BaseEntity
    {
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public bool IsArchived { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public long TeamId { get; set; }

        public Team Team { get; set; } = null!;

        public string CreatedByUserId { get; set; } = null!;

        public ApplicationUser CreatedByUser { get; set; } = null!;

        public ICollection<TaskItem> Tasks { get; set; }
            = new List<TaskItem>();

        // NEW: who can access/belong to this Project - see the Membership System brief.
        public ICollection<ProjectMember> ProjectMembers { get; set; }
            = new List<ProjectMember>();
    }
}
