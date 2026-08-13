using TaskManager.Data.Enums;

namespace TaskManager.Data.Entities
{
    public class Workspace : BaseEntity
    {
        public string Name { get; set; } = null!;

        public string Slug { get; set; } = null!;

        public string? Description { get; set; }

        public string? LogoUrl { get; set; }

        public WorkspaceStatus Status { get; set; } = WorkspaceStatus.Active;

        public ICollection<WorkspaceMember> Members { get; set; }
            = new List<WorkspaceMember>();

        public ICollection<Team> Teams { get; set; }
            = new List<Team>();

        public ICollection<Project> Projects { get; set; }
            = new List<Project>();

        public ICollection<Invitation> Invitations { get; set; }
            = new List<Invitation>();
    }
}
