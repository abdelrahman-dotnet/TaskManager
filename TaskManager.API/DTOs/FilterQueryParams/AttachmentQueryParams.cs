using TaskManager.API.DTOs.Params;
using TaskManager.API.Enums.SortingFields;

namespace TaskManager.API.DTOs.FilterQueryParams
{
    public class AttachmentQueryParams : CommonQueryParams
    {
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public long? TaskItemId { get; set; }
        public string? UploadedByUserId { get; set; }
        public DateTime? CreatedAt { get; set; }

        public List<SortOption<AttachmentSortingFields>> Sorts { get; set; } = new();
    }
}
