using TaskManager.API.DTOs.Params;
using TaskManager.API.Enums.SortingFields;

namespace TaskManager.API.DTOs.FilterQueryParams
{
    public class CommentQueryParams : CommonQueryParams
    {
        public string? Content { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public long? TaskId { get; set; }
        public string? UserId { get; set; }

        public List<SortOption<CommentSortingFields>> Sorts { get; set; } = new();
    }
}
