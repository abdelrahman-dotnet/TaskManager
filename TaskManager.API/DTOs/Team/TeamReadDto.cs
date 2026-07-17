namespace TaskManager.API.DTOs.Team
{
    public class TeamReadDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string ManagerId { get; set; } = null!;
        public string? ManagerName { get; set; }
        public int MembersCount { get; set; }
        public int ProjectsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}