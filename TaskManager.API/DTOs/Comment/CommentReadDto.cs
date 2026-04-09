namespace TaskManager.API.DTOs.Comment
{
    public class CommentReadDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public int TaskItemId { get; set; }
        public string UserId { get; set; } = null!;
    }
}