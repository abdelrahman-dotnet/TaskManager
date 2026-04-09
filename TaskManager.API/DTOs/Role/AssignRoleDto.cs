using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.DTOs.Role
{
    public class AssignRoleDto
    {
        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string RoleName { get; set; } = null!;
    }
}
