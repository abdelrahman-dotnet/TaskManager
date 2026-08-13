using TaskManager.Data.Entities;

namespace TaskManager.API.DTOs.ProjectMember
{
    public class ChangeProjectMemberRoleDto
    {
        public WorkspaceRole NewRole { get; set; }
    }
}
