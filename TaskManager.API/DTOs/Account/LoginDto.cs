using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.DTOs.Account
{
    public class LoginDto
    {
        [Required (ErrorMessage = "UserName Is Required")]
        [StringLength(100,MinimumLength = 3)]
        public string UserName { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = null!;
    }
}
