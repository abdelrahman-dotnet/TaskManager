namespace TaskManager.Data.Entities
{
    public class Project : BaseEntity
    {
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public bool IsArchived { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public long WorkspaceId { get; set; }
        public Workspace Workspace { get; set; } = null!;

        public string CreatedByUserId { get; set; } = null!;
        public ApplicationUser CreatedByUser { get; set; } = null!;

        public ICollection<ProjectMember> ProjectMembers { get; set; }
            = new List<ProjectMember>();

        public ICollection<ProjectTeam> ProjectTeams { get; set; }
            = new List<ProjectTeam>();

        public ICollection<TaskItem> Tasks { get; set; }
            = new List<TaskItem>();
    }
}
