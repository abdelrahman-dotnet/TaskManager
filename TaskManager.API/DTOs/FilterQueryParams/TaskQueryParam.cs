using TaskManager.API.DTOs.Params;
using TaskManager.API.Enums.SortingFields;
using TaskManager.Data.Entities;

namespace TaskManager.API.DTOs.FilterQueryParams
{
    public class TaskQueryParam : CommonQueryParams
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public TaskItemStatus? Status { get; set; }
        public TaskPriority? Priority { get; set; }
        public long? ProjectId { get; set; }
        public string? CreatedByUserId { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CreatedAt { get; set; }

        public List<SortOption<TaskSortingFields>> Sorts { get; set; } = new();
    }
}
