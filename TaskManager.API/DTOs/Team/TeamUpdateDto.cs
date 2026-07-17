namespace TaskManager.API.DTOs.Team
{
    public class TeamUpdateDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string ManagerId { get; set; } = null!;
    }
}
