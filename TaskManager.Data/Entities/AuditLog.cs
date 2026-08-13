namespace TaskManager.Data.Entities
{
    public class AuditLog : BaseEntity
    {
        public long WorkspaceId { get; set; }
        public Workspace Workspace { get; set; } = null!;

        public long? WorkspaceMemberId { get; set; }
        public WorkspaceMember? WorkspaceMember { get; set; }

        public string Action { get; set; } = null!;

        public string EntityName { get; set; } = null!;

        public string EntityId { get; set; } = null!;

        public string? OldValues { get; set; }

        public string? NewValues { get; set; }
    }
}
