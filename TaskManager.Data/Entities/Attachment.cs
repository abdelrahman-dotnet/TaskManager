using System.ComponentModel.DataAnnotations;

namespace TaskManager.Data.Entities
{
    public class Attachment : BaseEntity
    {
        public long TaskItemId { get; set; }

        public TaskItem TaskItem { get; set; } = null!;

        public string FileName { get; set; } = null!;

        public string StoredFileName { get; set; } = null!;

        public string FilePath { get; set; } = null!;

        public string ContentType { get; set; } = null!;

        public long FileSize { get; set; }

        public long UploadedByWorkspaceMemberId { get; set; }

        public WorkspaceMember UploadedByWorkspaceMember { get; set; } = null!;
    }
}
