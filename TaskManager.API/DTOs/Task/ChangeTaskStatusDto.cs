using TaskManager.Data.Entities;
namespace TaskManager.API.DTOs.Task
{
    public class ChangeTaskItemStatusDto
    {
        public TaskItemStatus NewStatus { get; set; }
    }
}
