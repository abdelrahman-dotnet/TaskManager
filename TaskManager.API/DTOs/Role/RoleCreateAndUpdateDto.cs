using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.DTOs.Role
{
    public class RoleCreateAndUpdateDto
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = null!;
        [MaxLength(200)]
        public string? Description { get; set; }
    }
}
