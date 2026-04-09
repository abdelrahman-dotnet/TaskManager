using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.DTOs.Account
{
    public class RefreshRequestDto
    {
        [Required(ErrorMessage = "Refresh token is required")]
        public string RefreshToken { get; set; } = null!;
    }
}
