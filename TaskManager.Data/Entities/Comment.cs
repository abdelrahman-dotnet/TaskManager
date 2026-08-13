namespace TaskManager.Data.Entities
{
    public class Comment : BaseEntity
    {
        public long TaskItemId { get; set; }

        public TaskItem TaskItem { get; set; } = null!;

        public long WorkspaceMemberId { get; set; }

        public WorkspaceMember WorkspaceMember { get; set; } = null!;

        public string Content { get; set; } = null!;
    }
}
