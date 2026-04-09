namespace TaskManager.API.DTOs.TaskItem
{
    public class TaskUpdateDto
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime DueDate { get; set; }
        //optional owner (can set from token later)
    }
}
