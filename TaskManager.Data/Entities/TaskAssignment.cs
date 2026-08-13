namespace TaskManager.Data.Entities
{
    public class TaskAssignment : BaseEntity
    {
        public long TaskItemId { get; set; }
        public TaskItem TaskItem { get; set; } = null!;

        public long WorkspaceMemberId { get; set; }
        public WorkspaceMember WorkspaceMember { get; set; } = null!;

        public long AssignedByWorkspaceMemberId { get; set; }
        public WorkspaceMember AssignedByWorkspaceMember { get; set; } = null!;

        // RESTORED (user-approved): audit timestamp for the assignment event.
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}
