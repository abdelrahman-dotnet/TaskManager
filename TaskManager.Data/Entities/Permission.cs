using System.ComponentModel.DataAnnotations;

namespace TaskManager.Data.Entities
{
    public class Permission
    {
        public int Id { get; set; }

        // MaxLength is required here: this column has a unique index in
        // AppDbContext, and SQL Server cannot index an nvarchar(max) column.
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(300)]
        public string? Description { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; }
            = new List<RolePermission>();
    }
}
