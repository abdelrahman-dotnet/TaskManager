using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Data.Entities;

namespace TaskManager.Data.Configurations
{
    public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
    {
        public void Configure(EntityTypeBuilder<Attachment> builder)
        {
            builder.HasKey(a => a.Id);

            builder.Property(a => a.FileName).IsRequired().HasMaxLength(255);
            builder.Property(a => a.StoredFileName).IsRequired().HasMaxLength(255);
            builder.Property(a => a.FilePath).IsRequired().HasMaxLength(500);
            builder.Property(a => a.ContentType).IsRequired().HasMaxLength(100);

            builder.HasQueryFilter(a => !a.IsDeleted);

            // Relationships
            builder.HasOne(a => a.TaskItem)
                .WithMany(t => t.Attachments)
                .HasForeignKey(a => a.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.UploadedByWorkspaceMember)
                .WithMany(wm => wm.UploadedAttachments)
                .HasForeignKey(a => a.UploadedByWorkspaceMemberId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
