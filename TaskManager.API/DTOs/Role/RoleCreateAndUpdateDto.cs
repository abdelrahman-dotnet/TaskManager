namespace TaskManager.API.DTOs.Role
{
    public class RoleCreateAndUpdateDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }
}