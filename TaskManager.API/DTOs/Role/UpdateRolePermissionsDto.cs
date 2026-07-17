namespace TaskManager.API.DTOs.Role
{
    public class UpdateRolePermissionsDto
    {
        public List<int> PermissionIds { get; set; } = new();
    }
}
