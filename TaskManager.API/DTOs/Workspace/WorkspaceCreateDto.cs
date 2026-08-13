namespace TaskManager.API.DTOs.Workspace
{
    public class WorkspaceCreateDto
    {
        public string Name { get; set; } = null!;

        public string? Description { get; set; }
    }
}