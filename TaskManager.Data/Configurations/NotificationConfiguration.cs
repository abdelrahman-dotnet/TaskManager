using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Data.Entities;

namespace TaskManager.Data.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasKey(n => n.Id);

            builder.Property(n => n.Title).IsRequired().HasMaxLength(150);
            builder.Property(n => n.Message).IsRequired();
            builder.Property(n => n.IsRead).HasDefaultValue(false);
            builder.HasQueryFilter(n => !n.IsDeleted);

            builder.HasIndex(n => new { n.WorkspaceMemberId, n.IsRead });

            // Relationships
            builder.HasOne(n => n.WorkspaceMember)
                .WithMany(wm => wm.Notifications)
                .HasForeignKey(n => n.WorkspaceMemberId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
