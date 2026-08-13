using TaskManager.API.DTOs.Params;
using TaskManager.API.Enums.SortingFields;

namespace TaskManager.API.DTOs.FilterQueryParams
{
    public class TaskItemStatusHistoryQueryParams : CommonQueryParams
    {
        public long? TaskItemId { get; set; }
        public TaskItemStatus? OldStatus { get; set; }
        public TaskItemStatus? NewStatus { get; set; }
        public string? ChangedByUserId { get; set; }
        public DateTime? ChangedAt { get; set; }

        public List<SortOption<TaskItemStatusHistorySortingFields>> Sorts { get; set; } = new();
    }
}
