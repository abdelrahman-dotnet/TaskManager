namespace TaskManager.Data.Entities
{
    public class ProjectTeam : BaseEntity
    {
        public long ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public long TeamId { get; set; }
        public Team Team { get; set; } = null!;
    }
}
