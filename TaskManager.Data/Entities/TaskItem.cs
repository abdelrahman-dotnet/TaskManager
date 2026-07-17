using System.ComponentModel.DataAnnotations;

namespace TaskManager.Data.Entities
{
    public class TaskItem : BaseEntity
    {
        [MaxLength(200)]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public TaskItemStatus Status { get; set; }

        public TaskPriority Priority { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime? CompletedAt { get; set; }

        public long ProjectId { get; set; }

        public Project Project { get; set; } = null!;

        public string CreatedByUserId { get; set; } = null!;

        public ApplicationUser CreatedByUser { get; set; } = null!;

        public ICollection<TaskAssignment> Assignments { get; set; }
            = new List<TaskAssignment>();

        public ICollection<Comment> Comments { get; set; }
            = new List<Comment>();

        public ICollection<Attachment> Attachments { get; set; }
            = new List<Attachment>();

        public ICollection<TaskStatusHistory> StatusHistory { get; set; }
            = new List<TaskStatusHistory>();
    }
}
