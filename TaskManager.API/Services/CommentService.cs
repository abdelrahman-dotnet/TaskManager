using TaskManager.API.Exceptions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using TaskManager.API.Config;
using TaskManager.API.Config.FiltersConfigs;
using TaskManager.API.DTOs.Comment;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.Extentions;
using TaskManager.API.Helpers;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Business.UnitOfWork;
using TaskManager.Data.Entities;

namespace TaskManager.API.Services
{
    public class CommentService : ICommentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CommentService> _logger;

        public CommentService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<CommentService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PagedResult<CommentReadDto>> GetAllAsync(CommentQueryParams queryParams, CancellationToken cancellationToken = default)
        {
            var query = _unitOfWork.Comments.GetAllQuery().AsNoTracking();

            query = query.ApplyFiltering(queryParams, CommentFilterConfig.map);

            if (!string.IsNullOrWhiteSpace(queryParams.Search))
            {
                var search = queryParams.Search;
                query = query.Where(c => c.Content.Contains(search));
            }

            // NOTE: was missing the tie-breaker used everywhere else (x => x.Id), which made
            // Skip/Take non-deterministic whenever the client sends no explicit sort.
            query = query.ApplySorting(queryParams.Sorts, AllowedSortingFields.Comments, x => x.Id);

            var projected = query.ProjectTo<CommentReadDto>(_mapper.ConfigurationProvider);
            var result = await projected.ToPagedResultAsync(queryParams.Page, queryParams.PageSize, cancellationToken);

            _logger.LogInformation("Comments retrieved successfully. Count: {Count}", result.Data.Count);
            return result;
        }

        public async Task<IEnumerable<CommentReadDto>> GetByTaskIdAsync(long taskId, CancellationToken cancellationToken = default)
        {
            var comments = await _unitOfWork.Comments.GetAllQuery()
                .AsNoTracking()
                .Where(c => c.TaskItemId == taskId)
                .OrderByDescending(c => c.CreatedAt)
                .ProjectTo<CommentReadDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            if (!comments.Any())
                _logger.LogWarning("No comments found for TaskId: {TaskId}", taskId);

            return comments;
        }

        public async Task<CommentReadDto> CreateAsync(long taskId, CommentCreateDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            var taskExists = await _unitOfWork.Tasks.ExistsAsync(t => t.Id == taskId, cancellationToken);
            if (!taskExists)
            {
                _logger.LogWarning("CreateComment failed. Task not found. TaskId: {TaskId}", taskId);
                throw new NotFoundException("Task not found.");
            }

            var comment = _mapper.Map<Comment>(dto);
            comment.TaskItemId = taskId;
            comment.UserId = currentUserId;

            await _unitOfWork.Comments.AddAsync(comment, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Comment created successfully. CommentId: {CommentId}, TaskId: {TaskId}, UserId: {UserId}",
                comment.Id, taskId, currentUserId);

            return _mapper.Map<CommentReadDto>(comment);
        }

        public async Task<CommentReadDto> UpdateAsync(long id, CommentUpdateDto dto, bool canManageAny, string currentUserId, CancellationToken cancellationToken = default)
        {
            var comment = await _unitOfWork.Comments.GetByIdAsync(id, cancellationToken);
            if (comment == null)
            {
                _logger.LogWarning("UpdateComment failed. Comment not found. CommentId: {CommentId}", id);
                throw new NotFoundException("Comment not found.");
            }

            if (!canManageAny && comment.UserId != currentUserId)
            {
                _logger.LogWarning("UpdateComment forbidden. UserId: {UserId} tried to edit CommentId: {CommentId}",currentUserId,id);

                throw new ForbiddenException("You can only edit your own comments.");
            }

            _mapper.Map(dto, comment);
            comment.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Comments.Update(comment);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Comment updated successfully. CommentId: {CommentId}", id);
            return _mapper.Map<CommentReadDto>(comment);
        }

        public async Task DeleteAsync(long id, string currentUserId, bool canManageAny, CancellationToken cancellationToken = default)
        {
            var comment = await _unitOfWork.Comments.GetByIdAsync(id, cancellationToken);
            if (comment == null)
            {
                _logger.LogWarning("DeleteComment failed. Not found. CommentId: {CommentId}", id);
                throw new NotFoundException("Comment not found.");
            }

            if (!canManageAny && comment.UserId != currentUserId)
            {
                _logger.LogWarning("DeleteComment forbidden. UserId: {UserId} tried to delete CommentId: {CommentId}", currentUserId, id);
                throw new ForbiddenException("You can only delete your own comments.");
            }

            _unitOfWork.Comments.Delete(comment);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Comment deleted successfully. CommentId: {CommentId}", id);
        }
    }
}