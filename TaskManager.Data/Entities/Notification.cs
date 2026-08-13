using System.ComponentModel.DataAnnotations;
using TaskManager.Data.Enums;

namespace TaskManager.Data.Entities
{
    public class Notification : BaseEntity
    {
        public long WorkspaceMemberId { get; set; }
        public WorkspaceMember WorkspaceMember { get; set; } = null!;

        [MaxLength(150)]
        public string Title { get; set; } = null!;

        public string Message { get; set; } = null!;

        public bool IsRead { get; set; }

        public NotificationType Type { get; set; }

        public long? ReferenceId { get; set; }
    }
}
