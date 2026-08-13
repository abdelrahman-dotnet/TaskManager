using TaskManager.Data.Entities;

namespace TaskManager.API.DTOs.TeamMember
{
    public class ChangeTeamMemberRoleDto
    {
        public WorkspaceRole NewRole { get; set; }
    }
}
