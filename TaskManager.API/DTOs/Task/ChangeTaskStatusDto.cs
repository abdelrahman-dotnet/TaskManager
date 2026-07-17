using TaskManager.Data.Entities;
namespace TaskManager.API.DTOs.Task
{
    public class ChangeTaskStatusDto
    {
        public TaskItemStatus NewStatus { get; set; }
    }
}
