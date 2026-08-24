using FluentValidation;
using System.Net.Http.Headers;
using TaskManager.API.DTOs.Attachment;

namespace TaskManager.API.Validators.Attachment
{
    public class AttachmentCreateValidator : AbstractValidator<AttachmentCreateDto>
    {
        private const long DefaultMaximumFileSizeBytes = 25L * 1024 * 1024;

        public AttachmentCreateValidator(IConfiguration configuration)
        {
            var maximumFileSizeBytes = configuration.GetValue<long?>("AttachmentUpload:MaxFileSizeBytes")
                ?? DefaultMaximumFileSizeBytes;

            RuleFor(x => x.TaskItemId)
                .GreaterThan(0).WithMessage("TaskItemId is required.");

            RuleFor(x => x.FileName)
                .NotEmpty().WithMessage("FileName is required.")
                .MaximumLength(255)
                .Must(IsSafeFileName).WithMessage("FileName must be a file name without path segments or control characters.");

            RuleFor(x => x.StoredFileName)
                .NotEmpty().WithMessage("StoredFileName is required.")
                .MaximumLength(255)
                .Must(IsSafeFileName).WithMessage("StoredFileName must be a file name without path segments or control characters.");

            RuleFor(x => x.FilePath)
                .NotEmpty().WithMessage("FilePath is required.")
                .MaximumLength(500)
                .Must(IsSafeRelativeStoragePath).WithMessage("FilePath must be a relative storage path without traversal segments.");

            RuleFor(x => x.ContentType)
                .NotEmpty().WithMessage("ContentType is required.")
                .MaximumLength(100)
                .Must(IsValidMediaType).WithMessage("ContentType must be a valid media type.");

            RuleFor(x => x.FileSize)
                .GreaterThan(0).WithMessage("FileSize must be greater than zero.")
                .LessThanOrEqualTo(maximumFileSizeBytes)
                .WithMessage($"FileSize must not exceed {maximumFileSizeBytes} bytes.");
        }

        private static bool IsSafeFileName(string? fileName)
        {
            return !string.IsNullOrWhiteSpace(fileName)
                && Path.GetFileName(fileName) == fileName
                && !fileName.Any(char.IsControl);
        }

        private static bool IsSafeRelativeStoragePath(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)
                || Path.IsPathRooted(filePath)
                || filePath.Any(char.IsControl))
            {
                return false;
            }

            var segments = filePath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            return segments.Length > 0 && segments.All(segment => segment is not "." and not "..");
        }

        private static bool IsValidMediaType(string? contentType)
        {
            return !string.IsNullOrWhiteSpace(contentType)
                && MediaTypeHeaderValue.TryParse(contentType, out _);
        }
    }
}
