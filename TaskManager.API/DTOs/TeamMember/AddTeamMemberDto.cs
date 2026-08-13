using TaskManager.Data.Entities;

namespace TaskManager.API.DTOs.TeamMember
{
    public class AddTeamMemberDto
    {
        public string UserId { get; set; } = null!;
        public WorkspaceRole Role { get; set; }
    }
}
