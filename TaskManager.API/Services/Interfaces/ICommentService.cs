using TaskManager.API.DTOs.Comment;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.Helpers;

namespace TaskManager.Business.Services.Interfaces
{
    public interface ICommentService
    {
        Task<PagedResult<CommentReadDto>> GetAllAsync(CommentQueryParams queryParams, CancellationToken cancellationToken = default);
        Task<IEnumerable<CommentReadDto>> GetByTaskIdAsync(long taskId, CancellationToken cancellationToken = default);
        Task<CommentReadDto> CreateAsync(long taskId, CommentCreateDto dto, string currentUserId, CancellationToken cancellationToken = default);
        Task<CommentReadDto> UpdateAsync(long id, CommentUpdateDto dto, bool canManageAny, string currentUserId, CancellationToken cancellationToken = default);
        Task DeleteAsync(long id, string currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    }
}