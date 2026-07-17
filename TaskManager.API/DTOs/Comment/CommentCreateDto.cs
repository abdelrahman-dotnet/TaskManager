namespace TaskManager.API.DTOs.Comment
{
    public class CommentCreateDto
    {
        public long TaskItemId { get; set; }
        public string Content { get; set; } = null!;
    }
}
