namespace TaskManager.Data.Entities
{
    public class TeamMember : BaseEntity
    {
        public long TeamId { get; set; }
        public Team Team { get; set; } = null!;

        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        public MembershipRole Role { get; set; }

        // CreatedAt (when they joined) already comes from BaseEntity - no need to duplicate it.
    }
}
