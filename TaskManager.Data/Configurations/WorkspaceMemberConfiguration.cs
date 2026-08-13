using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Data.Entities;
using TaskManager.Data.Enums;

namespace TaskManager.Data.Configurations
{
    public class WorkspaceMemberConfiguration : IEntityTypeConfiguration<WorkspaceMember>
    {
        public void Configure(EntityTypeBuilder<WorkspaceMember> builder)
        {
            builder.HasKey(wm => wm.Id);

            builder.Property(wm => wm.Status)
                .HasDefaultValue(WorkspaceMemberStatus.Active);

            builder.HasIndex(wm => new { wm.WorkspaceId, wm.UserId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.HasQueryFilter(wm => !wm.IsDeleted);

            // Relationships
            builder.HasOne(wm => wm.Workspace)
                .WithMany(w => w.Members)
                .HasForeignKey(wm => wm.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(wm => wm.User)
                .WithMany(u => u.WorkspaceMemberships)
                .HasForeignKey(wm => wm.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Other relationships (Tasks, Comments, etc.) are configured from the dependent side.
        }
    }
}
