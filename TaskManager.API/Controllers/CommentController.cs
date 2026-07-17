using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.API.Authorization;
using TaskManager.API.DTOs.Comment;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.Helpers;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Bussiness.Caching;
using TaskManager.Bussiness.Services;

namespace TaskManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CommentController : ControllerBase
    {
        private readonly ICommentService _commentService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CommentController> _logger;
        private readonly ICurrentUserService _currentUser;

        public CommentController(ICommentService commentService, ICacheService cacheService, ILogger<CommentController> logger, ICurrentUserService currentUser)
        {
            _commentService = commentService;
            _cacheService = cacheService;
            _logger = logger;
            _currentUser = currentUser;
        }

        private string CurrentUserId => _currentUser.UserId!;

        // No dedicated "Comments.View" permission exists in Permissions.cs, so these two
        // stay under the class-level plain [Authorize] - any authenticated user can read comments.
        [HttpGet]
        public async Task<IActionResult> GetAllComments([FromQuery] CommentQueryParams q, CancellationToken cancellationToken)
        {
            var version = await _cacheService.GetVersionAsync(CacheDomains.Comments);
            var cacheKey = CachKeyHelper.GenerateKey(CachePrefixes.CommentsList, version, q);

            var cached = await _cacheService.GetAsync<PagedResult<CommentReadDto>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Comment cache hit. CacheKey: {CacheKey}", cacheKey);
                return Ok(cached);
            }

            _logger.LogInformation("Comment cache miss. CacheKey: {CacheKey}", cacheKey);
            var result = await _commentService.GetAllAsync(q, cancellationToken);
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return Ok(result);
        }

        [HttpGet("tasks/{taskId}/comments")]
        public async Task<IActionResult> GetByTask(long taskId, CancellationToken cancellationToken)
        {
            var version = await _cacheService.GetVersionAsync(CacheDomains.Comments);
            var cacheKey = CachKeyHelper.GenerateKey(CachePrefixes.CommentsByTask, version, taskId);

            var cached = await _cacheService.GetAsync<IEnumerable<CommentReadDto>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Comments by Task cache HIT. TaskId: {TaskId}", taskId);
                return Ok(cached);
            }

            _logger.LogInformation("Comments by Task cache MISS. TaskId: {TaskId}", taskId);
            var comments = await _commentService.GetByTaskIdAsync(taskId, cancellationToken);

            await _cacheService.SetAsync(cacheKey, comments, TimeSpan.FromMinutes(5));
            return Ok(comments);
        }

        [HttpPost("tasks/{taskId}/comments")]
        [Authorize(Policy = Permissions.CommentsCreate)]
        public async Task<IActionResult> Create(long taskId, [FromBody] CommentCreateDto dto, CancellationToken cancellationToken)
        {
            var created = await _commentService.CreateAsync(taskId, dto, CurrentUserId, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Comments);

            return CreatedAtAction(nameof(GetByTask), new { taskId }, created);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = Permissions.CommentsUpdate)]
        public async Task<IActionResult> UpdateComment(long id, [FromBody] CommentUpdateDto dto,bool canManageAny, CancellationToken cancellationToken)
        {
            // NOTE: CommentsUpdate only gates whether the endpoint can be called at all.
            // CommentService.UpdateAsync still enforces "only the author can edit" -
            // there's no CommentsManageAny-style bypass for Update, only for Delete.
            var updated = await _commentService.UpdateAsync(id, dto,canManageAny , CurrentUserId, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Comments);

            return Ok(updated);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = Permissions.CommentsDelete)]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            var isAdmin = _currentUser.HasPermission(Permissions.CommentsManageAny);
            await _commentService.DeleteAsync(id, CurrentUserId, isAdmin, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Comments);

            return NoContent();
        }
    }
}