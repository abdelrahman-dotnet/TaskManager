using TaskManager.API.DTOs.Params;
using TaskManager.API.Enums.SortingFields;

namespace TaskManager.API.DTOs.FilterQueryParams
{
    public class AuditLogQueryParams : CommonQueryParams
    {
        [System.ComponentModel.DataAnnotations.Range(1, long.MaxValue, ErrorMessage = "WorkspaceId must be greater than zero")]
        public long WorkspaceId { get; set; }

        public string? Action { get; set; }
        public string? EntityName { get; set; }
        public string? EntityId { get; set; }
        public string? UserId { get; set; }
        public DateTime? CreatedAt { get; set; }

        public List<SortOption<AuditLogSortingFields>> Sorts { get; set; } = new();
    }
}
