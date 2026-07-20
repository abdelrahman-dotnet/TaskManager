using TaskManager.API.DTOs.Attachment;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.Helpers;

namespace TaskManager.Business.Services.Interfaces
{
    public interface IAttachmentService
    {
        // MEMBERSHIP: currentUserId/canManageAny added - results are filtered to attachments
        // on tasks whose project the user is a member of, unless canManageAny
        // (Attachments.ManageAny) bypasses it.
        Task<PagedResult<AttachmentReadDto>> GetAllAsync(AttachmentQueryParams queryParams, string currentUserId, bool canManageAny, CancellationToken cancellationToken = default);
        Task<AttachmentReadDto> CreateAsync(AttachmentCreateDto dto, string currentUserId, CancellationToken cancellationToken = default);
        Task DeleteAsync(long id, string currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    }
}
