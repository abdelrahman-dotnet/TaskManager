using System.ComponentModel.DataAnnotations;
using TaskManager.Data.Enums;

namespace TaskManager.Data.Entities
{
    public class TaskItem : BaseEntity
    {
        [MaxLength(200)]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public TaskItemStatus Status { get; set; }

        public TaskPriority Priority { get; set; }

        /// <summary>
        /// Used for ordering tasks inside the board/list.
        /// </summary>
        public int Position { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime? CompletedAt { get; set; }

        public bool IsArchived { get; set; }

        public long ProjectId { get; set; }

        public Project Project { get; set; } = null!;

        /// <summary>
        /// The platform user who originally created this task.
        /// Creation belongs to the user identity, not the workspace membership.
        /// </summary>
        public string CreatedByUserId { get; set; } = null!;

        public ApplicationUser CreatedByUser { get; set; } = null!;
        public long CreatedByWorkspaceMemberId { get; set; }

        public WorkspaceMember CreatedByWorkspaceMember { get; set; } = null!;
        public ICollection<TaskAssignment> Assignments { get; set; }
            = new List<TaskAssignment>();

        public ICollection<Comment> Comments { get; set; }
            = new List<Comment>();

        public ICollection<Attachment> Attachments { get; set; }
            = new List<Attachment>();

        public ICollection<TaskItemStatusHistory> StatusHistory { get; set; }
            = new List<TaskItemStatusHistory>();
    }
}
