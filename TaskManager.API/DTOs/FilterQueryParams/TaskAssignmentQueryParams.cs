using TaskManager.API.DTOs.Params;
using TaskManager.API.Enums.SortingFields;

namespace TaskManager.API.DTOs.FilterQueryParams
{
    public class TaskAssignmentQueryParams : CommonQueryParams
    {
        public long? TaskItemId { get; set; }
        public string? UserId { get; set; }
        public string? AssignedByUserId { get; set; }
        public DateTime? AssignedAt { get; set; }

        public List<SortOption<TaskAssignmentSortingFields>> Sorts { get; set; } = new();
    }
}
