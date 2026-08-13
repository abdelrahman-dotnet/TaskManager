using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Data.Entities;
using TaskManager.Data.Enums;

namespace TaskManager.Data.Configurations
{
    public class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
    {
        public void Configure(EntityTypeBuilder<Workspace> builder)
        {
            builder.HasKey(w => w.Id);

            builder.Property(w => w.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(w => w.Slug)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(w => w.Status)
                .HasDefaultValue(WorkspaceStatus.Active);

            builder.HasIndex(w => w.Slug)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.HasQueryFilter(w => !w.IsDeleted);

            // Relationships are configured from the dependent side (WorkspaceMember, Team, Project, Invitation, AuditLog)
        }
    }
}
