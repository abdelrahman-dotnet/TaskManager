namespace TaskManager.API.DTOs.Team
{
    public class TeamCreateDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }
}
