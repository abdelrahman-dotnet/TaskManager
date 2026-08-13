using TaskManager.Data.Entities;

namespace TaskManager.API.DTOs.ProjectMember
{
    public class ProjectMemberReadDto
    {
        public long Id { get; set; }
        public long ProjectId { get; set; }
        public string UserId { get; set; } = null!;
        public string? UserName { get; set; }
        public WorkspaceRole Role { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
