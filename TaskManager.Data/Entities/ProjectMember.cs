namespace TaskManager.Data.Entities
{
    public class ProjectMember : BaseEntity
    {
        public long ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public long WorkspaceMemberId { get; set; }
        public WorkspaceMember WorkspaceMember { get; set; } = null!;
    }
}
