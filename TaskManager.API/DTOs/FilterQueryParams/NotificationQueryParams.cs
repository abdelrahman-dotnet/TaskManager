using TaskManager.API.DTOs.Params;
using TaskManager.API.Enums.SortingFields;

namespace TaskManager.API.DTOs.FilterQueryParams
{
    public class NotificationQueryParams : CommonQueryParams
    {
        public string? Title { get; set; }
        public bool? IsRead { get; set; }
        public string? UserId { get; set; }
        public DateTime? CreatedAt { get; set; }

        public List<SortOption<NotificationSortingFields>> Sorts { get; set; } = new();
    }
}
