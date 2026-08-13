namespace TaskManager.API.DTOs.TaskItemStatusHistory
{
    public class TaskItemStatusHistoryReadDto
    {
        public long Id { get; set; }
        public long TaskItemId { get; set; }
        public TaskItemStatus OldStatus { get; set; }
        public TaskItemStatus NewStatus { get; set; }
        public string ChangedByUserId { get; set; } = null!;
        public DateTime ChangedAt { get; set; }
    }
}
