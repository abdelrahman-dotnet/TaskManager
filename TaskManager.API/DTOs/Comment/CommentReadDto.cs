namespace TaskManager.API.DTOs.Comment
{
    public class CommentReadDto
    {
        public long Id { get; set; }
        public long TaskItemId { get; set; }
        public string UserId { get; set; } = null!;
        public string? UserFullName { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
