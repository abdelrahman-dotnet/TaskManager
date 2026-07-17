namespace TaskManager.API.DTOs.Attachment
{
    // Metadata only - the actual file bytes are handled separately (e.g. via IFormFile in the controller,
    // saved to disk/blob storage there, then this DTO carries what the Service needs to persist).
    public class AttachmentCreateDto
    {
        public long TaskItemId { get; set; }
        public string FileName { get; set; } = null!;
        public string StoredFileName { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long FileSize { get; set; }
    }
}
