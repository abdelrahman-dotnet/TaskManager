namespace TaskManager.Data.Entities
{
    public class Team : BaseEntity
    {
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public long WorkspaceId { get; set; }
        public Workspace Workspace { get; set; } = null!;

        public ICollection<TeamMember> TeamMembers { get; set; }
            = new List<TeamMember>();

        public ICollection<ProjectTeam> ProjectTeams { get; set; }
            = new List<ProjectTeam>();
    }
}
