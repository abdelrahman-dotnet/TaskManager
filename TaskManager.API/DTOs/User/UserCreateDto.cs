using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.DTOs.User
{
    public class UserCreateDto
    {
        //public string FirstName { get; set; } = null!;
        public string UserName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string Password { get; set; } = null!;

        public long? TeamId { get; set; }
    }
}
