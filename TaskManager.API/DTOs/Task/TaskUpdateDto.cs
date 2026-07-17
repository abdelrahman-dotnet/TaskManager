namespace TaskManager.API.DTOs.Task
{
    public class TaskUpdateDto
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public long ProjectId { get; set; }
    }
}
