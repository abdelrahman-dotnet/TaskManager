namespace TaskManager.API.DTOs.Project
{
    public class ProjectReadDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsArchived { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<long> TeamIds { get; set; } = new();
        public List<string> TeamNames { get; set; } = new();
        public string CreatedByUserId { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}