namespace TaskManager.API.DTOs.User
{
    public class UserReadDto
    {
        public string Id { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public DateTime DateOfCreation { get; set; }
        public bool IsActive { get; set; }
    }
}
