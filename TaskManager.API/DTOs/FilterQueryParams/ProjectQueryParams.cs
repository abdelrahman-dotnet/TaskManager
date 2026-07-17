using TaskManager.API.DTOs.Params;
using TaskManager.API.Enums.SortingFields;

namespace TaskManager.API.DTOs.FilterQueryParams
{
    public class ProjectQueryParams : CommonQueryParams
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool? IsArchived { get; set; }
        public long? TeamId { get; set; }
        public string? CreatedByUserId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? CreatedAt { get; set; }

        public List<SortOption<ProjectSortingFields>> Sorts { get; set; } = new();
    }
}
