namespace TaskManager.API.DTOs.Attachment
{
    public class AttachmentReadDto
    {
        public long Id { get; set; }
        public long TaskItemId { get; set; }
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long FileSize { get; set; }
        public string UploadedByUserId { get; set; } = null!;
        public string? UploadedByUserName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
