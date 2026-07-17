using TaskManager.API.DTOs.Params;
using TaskManager.API.Enums.SortingFields;

namespace TaskManager.API.DTOs.FilterQueryParams
{
    public class TeamQueryParams : CommonQueryParams
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ManagerId { get; set; }
        public DateTime? CreatedAt { get; set; }

        public List<SortOption<TeamSortingFields>> Sorts { get; set; } = new();
    }
}
