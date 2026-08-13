using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Data.Entities;

namespace TaskManager.Data.Configurations
{
    public class TaskItemStatusHistoryConfiguration : IEntityTypeConfiguration<TaskItemStatusHistory>
    {
        public void Configure(EntityTypeBuilder<TaskItemStatusHistory> builder)
        {
            builder.HasKey(h => h.Id);

            builder.HasQueryFilter(h => !h.IsDeleted);

            // Relationships
            builder.HasOne(h => h.TaskItem)
                .WithMany(t => t.StatusHistory)
                .HasForeignKey(h => h.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(h => h.ChangedByWorkspaceMember)
                .WithMany(wm => wm.StatusChanges)
                .HasForeignKey(h => h.ChangedByWorkspaceMemberId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
