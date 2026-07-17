using TaskManager.API.DTOs.Params;
using TaskManager.API.Enums.SortingFields;

namespace TaskManager.API.DTOs.FilterQueryParams
{
    public class TaskStatusHistoryQueryParams : CommonQueryParams
    {
        public long? TaskItemId { get; set; }
        public TaskStatus? OldStatus { get; set; }
        public TaskStatus? NewStatus { get; set; }
        public string? ChangedByUserId { get; set; }
        public DateTime? ChangedAt { get; set; }

        public List<SortOption<TaskStatusHistorySortingFields>> Sorts { get; set; } = new();
    }
}
