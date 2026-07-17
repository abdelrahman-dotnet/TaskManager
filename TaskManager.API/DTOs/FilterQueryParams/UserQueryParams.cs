using TaskManager.API.DTOs.Params;
using TaskManager.API.Enums.SortingFields;

namespace TaskManager.API.DTOs.FilterQueryParams
{
    public class UserQueryParams : CommonQueryParams
    {
        public string? UserName { get; set; }
        public bool? IsActive { get; set; }
        public long? TeamId { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime? CreatedAt { get; set; }

        public List<SortOption<UserSortingFields>> Sorts { get; set; } = new();
    }
}