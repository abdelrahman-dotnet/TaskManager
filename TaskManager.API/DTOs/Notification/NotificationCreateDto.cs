namespace TaskManager.API.DTOs.Notification
{
    // Used internally by other services (e.g. when a task is assigned) - not exposed to a public "create" endpoint.
    public class NotificationCreateDto
    {
        /// <summary>
        /// Workspace in which the notification is raised (used to scope visibility
        /// and to resolve the recipient's WorkspaceMember in that workspace).
        /// </summary>
        public long WorkspaceId { get; set; }

        /// <summary>
        /// String user id of the notification recipient.
        /// </summary>
        public string UserId { get; set; } = null!;

        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
    }
}
