using TaskManager.Data.Entities;
namespace TaskManager.API.DTOs.Task
{
    public class TaskReadDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public TaskItemStatus Status { get; set; }
        public TaskPriority Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public long? ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public string CreatedByUserId { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
