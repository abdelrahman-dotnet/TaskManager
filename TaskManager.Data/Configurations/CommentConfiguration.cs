using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Data.Entities;

namespace TaskManager.Data.Configurations
{
    public class CommentConfiguration : IEntityTypeConfiguration<Comment>
    {
        public void Configure(EntityTypeBuilder<Comment> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Content)
                .IsRequired();

            builder.HasQueryFilter(c => !c.IsDeleted);

            // Relationships
            builder.HasOne(c => c.TaskItem)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.WorkspaceMember)
                .WithMany(wm => wm.Comments)
                .HasForeignKey(c => c.WorkspaceMemberId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
