using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Data.Entities;

namespace TaskManager.Data.Configurations
{
    public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
    {
        public void Configure(EntityTypeBuilder<ProjectMember> builder)
        {
            builder.HasKey(pm => pm.Id);

            builder.HasIndex(pm => new { pm.ProjectId, pm.WorkspaceMemberId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.HasQueryFilter(pm => !pm.IsDeleted);

            // Relationships
            builder.HasOne(pm => pm.Project)
                .WithMany(p => p.ProjectMembers)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pm => pm.WorkspaceMember)
                .WithMany(wm => wm.ProjectMemberships)
                .HasForeignKey(pm => pm.WorkspaceMemberId)
                .OnDelete(DeleteBehavior.Restrict); // Changed to Restrict to avoid multiple cascade paths
        }
    }
}
