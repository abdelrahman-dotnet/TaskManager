using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Data.Entities;

namespace TaskManager.Data.Configurations
{
    public class TaskAssignmentConfiguration : IEntityTypeConfiguration<TaskAssignment>
    {
        public void Configure(EntityTypeBuilder<TaskAssignment> builder)
        {
            builder.HasKey(ta => ta.Id);

            builder.HasQueryFilter(ta => !ta.IsDeleted);

            // Relationships
            builder.HasOne(ta => ta.TaskItem)
                .WithMany(t => t.Assignments)
                .HasForeignKey(ta => ta.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ta => ta.WorkspaceMember)
                .WithMany(wm => wm.TaskAssignments)
                .HasForeignKey(ta => ta.WorkspaceMemberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ta => ta.AssignedByWorkspaceMember)
                .WithMany()
                .HasForeignKey(ta => ta.AssignedByWorkspaceMemberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(ta => new { ta.TaskItemId, ta.WorkspaceMemberId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        }
    }
}
