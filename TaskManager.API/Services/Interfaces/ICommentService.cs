using TaskManager.API.DTOs.Comment;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.Helpers;

namespace TaskManager.Business.Services.Interfaces
{
    public interface ICommentService
    {
        // MEMBERSHIP: currentUserId/canManageAny added - results are filtered to comments on
        // tasks whose project the user is a member of, unless canManageAny
        // (Comments.ManageAny) bypasses it.
        Task<PagedResult<CommentReadDto>> GetAllAsync(CommentQueryParams queryParams, string currentUserId, bool canManageAny, CancellationToken cancellationToken = default);

        Task<IEnumerable<CommentReadDto>> GetByTaskIdAsync(long taskId, string currentUserId, CancellationToken cancellationToken = default);
        Task<CommentReadDto> CreateAsync(long taskId, CommentCreateDto dto, string currentUserId, CancellationToken cancellationToken = default);
        Task<CommentReadDto> UpdateAsync(long id, CommentUpdateDto dto, bool canManageAny, string currentUserId, CancellationToken cancellationToken = default);
        Task DeleteAsync(long id, string currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    }
}
