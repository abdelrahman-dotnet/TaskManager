using TaskManager.Data.Enums;

namespace TaskManager.Data.Entities
{
    public class TaskItemStatusHistory : BaseEntity
    {
        public long TaskItemId { get; set; }

        public TaskItem TaskItem { get; set; } = null!;

        public TaskItemStatus OldStatus { get; set; }

        public TaskItemStatus NewStatus { get; set; }

        public long ChangedByWorkspaceMemberId { get; set; }

        public WorkspaceMember ChangedByWorkspaceMember { get; set; } = null!;

        // RESTORED (user-approved): audit timestamp for the status-change event.
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
