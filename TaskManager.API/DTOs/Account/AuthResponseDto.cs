namespace TaskManager.API.DTOs.Account
{
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = null!;
        public DateTime ExpiresAt ;
    }
}
