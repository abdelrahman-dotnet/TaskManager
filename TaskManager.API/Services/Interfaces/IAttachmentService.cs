using TaskManager.API.DTOs.Attachment;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.Helpers;

namespace TaskManager.Business.Services.Interfaces
{
    public interface IAttachmentService
    {
        Task<PagedResult<AttachmentReadDto>> GetAllAsync(AttachmentQueryParams queryParams, CancellationToken cancellationToken = default);
        Task<AttachmentReadDto> CreateAsync(AttachmentCreateDto dto, string currentUserId, CancellationToken cancellationToken = default);
        Task DeleteAsync(long id, string currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    }
}