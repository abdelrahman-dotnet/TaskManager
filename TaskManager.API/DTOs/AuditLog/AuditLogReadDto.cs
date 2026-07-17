namespace TaskManager.API.DTOs.AuditLog
{
    public class AuditLogReadDto
    {
        public long Id { get; set; }
        public string? UserId { get; set; }
        public string Action { get; set; } = null!;
        public string EntityName { get; set; } = null!;
        public string EntityId { get; set; } = null!;
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
