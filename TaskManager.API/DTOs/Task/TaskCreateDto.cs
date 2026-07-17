using TaskManager.Data.Entities;

namespace TaskManager.API.DTOs.Task
{
    public class TaskCreateDto
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public DateTime? DueDate { get; set; }
        public long ProjectId { get; set; }
    }
}
