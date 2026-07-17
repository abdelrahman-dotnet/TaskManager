namespace TaskManager.Data.Entities
{
    public class ProjectMember : BaseEntity
    {
        public long ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        public MembershipRole Role { get; set; }
    }
}
