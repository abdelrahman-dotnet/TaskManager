using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Data.Entities;

namespace TaskManager.Data.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Action).IsRequired().HasMaxLength(50);
            builder.Property(a => a.EntityName).IsRequired().HasMaxLength(100);
            builder.Property(a => a.EntityId).IsRequired().HasMaxLength(50);

            builder.HasQueryFilter(a => !a.IsDeleted);

            builder.HasIndex(a => new { a.WorkspaceId, a.EntityName, a.EntityId });

            // Relationships
            builder.HasOne(a => a.Workspace)
                .WithMany()
                .HasForeignKey(a => a.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.WorkspaceMember)
                .WithMany(wm => wm.AuditLogs)
                .HasForeignKey(a => a.WorkspaceMemberId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
