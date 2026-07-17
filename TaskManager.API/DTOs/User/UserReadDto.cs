namespace TaskManager.API.DTOs.User
{
    public class UserReadDto
    {
        public string Id { get; set; } = null!;
        //public string FirstName { get; set; } = null!;
        public string UserName { get; set; } = null!;

        public string? Email { get; set; }
        public bool IsActive { get; set; }
        public long? TeamId { get; set; }
        public string? TeamName { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
