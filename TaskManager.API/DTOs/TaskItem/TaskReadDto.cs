using TaskManager.API.DTOs.Comment;

namespace TaskManager.API.DTOs.TaskItem
{
    public class TaskReadDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime DueDate { get; set; }
        public string? UserId { get; set; }

        public List<CommentReadDto> Comments { get; set; }
    }
}
