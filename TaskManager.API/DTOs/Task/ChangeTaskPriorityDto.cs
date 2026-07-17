using TaskManager.Data.Entities;

namespace TaskManager.API.DTOs.Task
{
    public class ChangeTaskPriorityDto
    {
        public TaskPriority NewPriority { get; set; }
    }
}