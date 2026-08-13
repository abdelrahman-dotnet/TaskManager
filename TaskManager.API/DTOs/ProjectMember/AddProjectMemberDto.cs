using TaskManager.Data.Entities;

namespace TaskManager.API.DTOs.ProjectMember
{
    public class AddProjectMemberDto
    {
        public string UserId { get; set; } = null!;
        public WorkspaceRole Role { get; set; }
    }
}
