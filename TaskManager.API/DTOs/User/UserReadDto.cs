namespace TaskManager.API.DTOs.User
{
    public class UserReadDto
    {
        public string Id { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string? Email { get; set; }
        public bool IsActive { get; set; }
        public List<long> TeamIds { get; set; } = new();
        public List<string> TeamNames { get; set; } = new();
        public List<long> ProjectIds { get; set; } = new();
        public List<string> ProjectNames { get; set; } = new();
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}