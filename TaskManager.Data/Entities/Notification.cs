using System.ComponentModel.DataAnnotations;

namespace TaskManager.Data.Entities
{
    public class Notification : BaseEntity
    {
        public string UserId { get; set; } = null!;

        public ApplicationUser User { get; set; } = null!;

        [MaxLength(150)]
        public string Title { get; set; } = null!;

        public string Message { get; set; } = null!;

        public bool IsRead { get; set; }
    }
}
