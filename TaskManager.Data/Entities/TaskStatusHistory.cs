namespace TaskManager.Data.Entities
{
    public class TaskStatusHistory : BaseEntity
    {
        public long TaskItemId { get; set; }

        public TaskItem TaskItem { get; set; } = null!;

        public TaskItemStatus OldStatus { get; set; }

        public TaskItemStatus NewStatus { get; set; }

        public string ChangedByUserId { get; set; } = null!;

        public ApplicationUser ChangedByUser { get; set; } = null!;

        public DateTime ChangedAt { get; set; }
    }
}
