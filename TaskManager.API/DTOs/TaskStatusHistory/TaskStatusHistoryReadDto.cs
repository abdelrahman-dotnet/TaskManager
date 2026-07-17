namespace TaskManager.API.DTOs.TaskStatusHistory
{
    public class TaskStatusHistoryReadDto
    {
        public long Id { get; set; }
        public long TaskItemId { get; set; }
        public TaskStatus OldStatus { get; set; }
        public TaskStatus NewStatus { get; set; }
        public string ChangedByUserId { get; set; } = null!;
        public DateTime ChangedAt { get; set; }
    }
}
