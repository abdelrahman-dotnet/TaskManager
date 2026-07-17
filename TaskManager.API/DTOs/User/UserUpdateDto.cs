namespace TaskManager.API.DTOs.User
{
    // Profile fields only. Email/password changes go through dedicated Identity endpoints
    // (ChangeEmail/ChangePassword) since UserManager requires special token flows for those.
    public class UserUpdateDto
    {
        //public string FirstName { get; set; } = null!;
        public string UserName { get; set; } = null!;

        public long? TeamId { get; set; }
        public bool ShouldNotify { get; set; } = true;
        public int NotifyPeriod { get; set; }
    }
}
