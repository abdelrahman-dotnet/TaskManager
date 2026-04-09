using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.DTOs.User
{
    public class UserUpdateDto
    {
        [Required]
        [MaxLength(100)]
        public string? UserName { get; set; }
        [Required]
        [MaxLength(200)]
        public string? Email { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
}
