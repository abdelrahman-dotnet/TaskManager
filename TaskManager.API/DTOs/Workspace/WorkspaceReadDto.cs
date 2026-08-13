using TaskManager.Data.Enums;

namespace TaskManager.API.DTOs.Workspace
{
    public class WorkspaceReadDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public WorkspaceStatus Status { get; set; }
        public WorkspaceRole Role { get; set; }
    }
}