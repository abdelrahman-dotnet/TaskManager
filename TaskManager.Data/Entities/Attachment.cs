using System.ComponentModel.DataAnnotations;

namespace TaskManager.Data.Entities
{
    public class Attachment : BaseEntity
    {
        public long TaskItemId { get; set; }

        public TaskItem TaskItem { get; set; } = null!;

        [MaxLength(255)]
        public string FileName { get; set; } = null!;

        [MaxLength(255)]
        public string StoredFileName { get; set; } = null!;

        [MaxLength(500)]
        public string FilePath { get; set; } = null!;

        [MaxLength(100)]
        public string ContentType { get; set; } = null!;

        public long FileSize { get; set; }

        public string UploadedByUserId { get; set; } = null!;

        public ApplicationUser UploadedByUser { get; set; } = null!;
    }
}
