namespace TaskManager.API.DTOs.TaskAssignment
{
    public class TaskAssignmentReadDto
    {
        public long Id { get; set; }
        public long TaskItemId { get; set; }
        public string UserId { get; set; } = null!;
        public string? UserFullName { get; set; }
        public string AssignedByUserId { get; set; } = null!;
        public DateTime AssignedAt { get; set; }
    }
}
