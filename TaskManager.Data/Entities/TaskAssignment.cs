namespace TaskManager.Data.Entities
{
    public class TaskAssignment : BaseEntity
    {
        public long TaskItemId { get; set; }

        public TaskItem TaskItem { get; set; } = null!;

        public string UserId { get; set; } = null!;

        public ApplicationUser User { get; set; } = null!;

        public string AssignedByUserId { get; set; } = null!;

        public ApplicationUser AssignedByUser { get; set; } = null!;

        public DateTime AssignedAt { get; set; }
    }
}
