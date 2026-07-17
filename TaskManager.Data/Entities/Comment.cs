namespace TaskManager.Data.Entities
{
    public class Comment : BaseEntity
    {
        public long TaskItemId { get; set; }

        public TaskItem TaskItem { get; set; } = null!;

        public string UserId { get; set; } = null!;

        public ApplicationUser User { get; set; } = null!;

        public string Content { get; set; } = null!;
    }
}
