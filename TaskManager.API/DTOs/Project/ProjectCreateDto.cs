namespace TaskManager.API.DTOs.Project
{
    public class ProjectCreateDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public long TeamId { get; set; }
    }
}
