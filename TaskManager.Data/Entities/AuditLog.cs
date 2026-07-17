using System.ComponentModel.DataAnnotations;

namespace TaskManager.Data.Entities
{
    public class AuditLog : BaseEntity
    {
        public string? UserId { get; set; }

        // Was "= null!" while UserId is nullable -> contradictory.
        // This relationship is optional (system/background actions may have
        // no user), so the navigation property must be nullable too.
        public ApplicationUser? User { get; set; }

        [MaxLength(100)]
        public string Action { get; set; } = null!;

        // Has a composite index with EntityId in AppDbContext -> needs MaxLength.
        [MaxLength(100)]
        public string EntityName { get; set; } = null!;

        [MaxLength(50)]
        public string EntityId { get; set; } = null!;

        public string? OldValues { get; set; }

        public string? NewValues { get; set; }
    }
}
