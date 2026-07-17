using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.DTOs.Account
{
    public class LoginDto
    {
       public string Email { get; set; } = null!;
       public string Password { get; set; } = null!;
    }
}
