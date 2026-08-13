using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Data.Entities;

namespace TaskManager.Data.Configurations
{
    public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
    {
        public void Configure(EntityTypeBuilder<Invitation> builder)
        {
            builder.HasKey(i => i.Id);

            builder.Property(i => i.InvitedUserId).IsRequired();

            builder.HasQueryFilter(i => !i.IsDeleted);

            // Relationships
            builder.HasOne(i => i.Workspace)
                .WithMany(w => w.Invitations)
                .HasForeignKey(i => i.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(i => i.InvitedUser)
                .WithMany(u => u.ReceivedInvitations)
                .HasForeignKey(i => i.InvitedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(i => i.InvitedByWorkspaceMember)
                .WithMany(wm => wm.SentInvitations)
                .HasForeignKey(i => i.InvitedByWorkspaceMemberId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
