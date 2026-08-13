namespace TaskManager.Data.Entities
{
    public class TeamMember : BaseEntity
    {
        public long TeamId { get; set; }
        public Team Team { get; set; } = null!;

        public long WorkspaceMemberId { get; set; }
        public WorkspaceMember WorkspaceMember { get; set; } = null!;
    }
}
