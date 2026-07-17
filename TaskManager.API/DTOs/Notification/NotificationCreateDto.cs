namespace TaskManager.API.DTOs.Notification
{
    // Used internally by other services (e.g. when a task is assigned) - not exposed to a public "create" endpoint.
    public class NotificationCreateDto
    {
        public string UserId { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
    }
}
