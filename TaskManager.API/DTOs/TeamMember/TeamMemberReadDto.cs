using TaskManager.Data.Entities;

namespace TaskManager.API.DTOs.TeamMember
{
    public class TeamMemberReadDto
    {
        public long Id { get; set; }
        public long TeamId { get; set; }
        public string UserId { get; set; } = null!;
        public string? UserName { get; set; }
        public WorkspaceRole Role { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
