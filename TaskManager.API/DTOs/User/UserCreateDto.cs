using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.DTOs.User
{
    public class UserCreateDto
    {
        [Required]
        [MaxLength(100)]
        public string UserName { get; set; } = null!;
        [Required]
        [MaxLength(200)]
        public string Email { get; set; } = null!;
        public bool IsActive { get; set; } = true;
    }
}
