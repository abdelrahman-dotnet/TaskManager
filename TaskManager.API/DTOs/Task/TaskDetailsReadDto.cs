namespace TaskManager.API.DTOs.Task
{
    public class TaskDetailsReadDto : TaskReadDto
    {
        public List<TaskAssignmentMiniDto> Assignments { get; set; } = new();
        public int CommentsCount { get; set; }
        public int AttachmentsCount { get; set; }
    }

    public class TaskAssignmentMiniDto
    {
        public string UserId { get; set; } = null!;
        public string? UserFullName { get; set; }
        public DateTime AssignedAt { get; set; }
    }
}
