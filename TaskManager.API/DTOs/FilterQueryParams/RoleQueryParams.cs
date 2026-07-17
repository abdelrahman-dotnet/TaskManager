using TaskManager.API.DTOs.Params;
using TaskManager.API.Enums.SortingFields;

namespace TaskManager.API.DTOs.FilterQueryParams
{
    public class RoleQueryParams : CommonQueryParams
    {
        public string? Name { get; set; }
        public string? Description { get; set; }

        public List<SortOption<RoleSortingFields>> Sorts { get; set; } = new();
    }
}
