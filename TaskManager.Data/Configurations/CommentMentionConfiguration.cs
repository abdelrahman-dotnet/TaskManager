using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Data.Entities;

namespace TaskManager.Data.Configurations
{
    public class CommentMentionConfiguration : IEntityTypeConfiguration<CommentMention>
    {
        public void Configure(EntityTypeBuilder<CommentMention> builder)
        {
            builder.HasKey(cm => cm.Id);

            builder.HasOne(cm => cm.Comment)
                .WithMany()
                .HasForeignKey(cm => cm.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(cm => cm.MentionedWorkspaceMember)
                .WithMany()
                .HasForeignKey(cm => cm.MentionedWorkspaceMemberId)
                .OnDelete(DeleteBehavior.Restrict);

            // One mention per user per comment.
            builder.HasIndex(cm => new { cm.CommentId, cm.MentionedWorkspaceMemberId })
                .IsUnique()
                .HasDatabaseName("IX_CommentMention_CommentId_MentionedWorkspaceMemberId");
        }
    }
}
