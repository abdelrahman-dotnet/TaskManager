using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.API.Authorization;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.Task;
using TaskManager.API.Helpers;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Bussiness.Caching;
using TaskManager.Bussiness.Services;

namespace TaskManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TaskController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<TaskController> _logger;
        private readonly ICurrentUserService _currentUser;

        public TaskController(ITaskService taskService, ICacheService cacheService, ILogger<TaskController> logger, ICurrentUserService currentUser)
        {
            _taskService = taskService;
            _cacheService = cacheService;
            _logger = logger;
            _currentUser = currentUser;
        }

        private string CurrentUserId => _currentUser.UserId!;
        private bool CanManageAny => _currentUser.HasPermission(Permissions.TasksManageAny);

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] TaskQueryParam q, CancellationToken cancellationToken)
        {
            var version = await _cacheService.GetVersionAsync(CacheDomains.Tasks);
            // MEMBERSHIP: results now depend on who's asking (visible projects differ per
            // user), so CurrentUserId/CanManageAny must be part of the cache key - otherwise
            // User A's cached page could be served to User B for the same query params.
            var cacheKey = CachKeyHelper.GenerateKey(CachePrefixes.TasksList, version, new { q, CurrentUserId, CanManageAny });

            var cached = await _cacheService.GetAsync<PagedResult<TaskReadDto>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Tasks cache hit. CacheKey: {CacheKey}", cacheKey);
                return Ok(cached);
            }

            _logger.LogInformation("Tasks cache miss. CacheKey: {CacheKey}", cacheKey);
            var result = await _taskService.GetAllAsync(q, CurrentUserId, CanManageAny, cancellationToken);
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            var version = await _cacheService.GetVersionAsync(CacheDomains.Tasks);
            var cacheKey = CachKeyHelper.GenerateKey(CachePrefixes.TaskByIdWithIncludes, version, new { id, CurrentUserId, CanManageAny });

            var cached = await _cacheService.GetAsync<TaskDetailsReadDto>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Task cache hit. TaskId: {TaskId}", id);
                return Ok(cached);
            }

            _logger.LogInformation("Task cache miss. TaskId: {TaskId}", id);
            var task = await _taskService.GetByIdAsync(id, CurrentUserId, CanManageAny, cancellationToken);
            await _cacheService.SetAsync(cacheKey, task, TimeSpan.FromMinutes(5));

            return Ok(task);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.TasksCreate)]
        public async Task<IActionResult> Create([FromBody] TaskCreateDto dto, CancellationToken cancellationToken)
        {
            var created = await _taskService.CreateAsync(dto, CurrentUserId, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Tasks);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = Permissions.TasksUpdate)]
        public async Task<IActionResult> Update(long id, [FromBody] TaskUpdateDto dto, CancellationToken cancellationToken)
        {
            var updated = await _taskService.UpdateAsync(id, dto, CurrentUserId, CanManageAny, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Tasks);

            return Ok(updated);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = Permissions.TasksDelete)]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            await _taskService.DeleteAsync(id, CurrentUserId, CanManageAny, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Tasks);

            return NoContent();
        }

        [HttpPost("{id}/assign")]
        [Authorize(Policy = Permissions.TasksAssign)]
        public async Task<IActionResult> Assign(long id, [FromBody] AssignTaskDto dto, CancellationToken cancellationToken)
        {
            var result = await _taskService.AssignAsync(id, dto, CurrentUserId, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Tasks);
            await _cacheService.IncrementVersionAsync(CacheDomains.TaskAssignments);

            return Ok(result);
        }

        [HttpDelete("{id}/assign/{userId}")]
        [Authorize(Policy = Permissions.TasksAssign)]
        public async Task<IActionResult> Unassign(long id, string userId, CancellationToken cancellationToken)
        {
            var result = await _taskService.UnassignAsync(id, CurrentUserId, userId, cancellationToken);

            await _cacheService.IncrementVersionAsync(CacheDomains.Tasks);
            await _cacheService.IncrementVersionAsync(CacheDomains.TaskAssignments);
            return Ok(result);
        }

        [HttpPatch("{id}/status")]
        [Authorize(Policy = Permissions.TasksUpdate)]
        public async Task<IActionResult> ChangeStatus(long id, [FromBody] ChangeTaskStatusDto dto, CancellationToken cancellationToken)
        {
            var result = await _taskService.ChangeStatusAsync(id, dto, CurrentUserId, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Tasks);
            await _cacheService.IncrementVersionAsync(CacheDomains.TaskStatusHistories);

            return Ok(result);
        }

        [HttpPatch("{id}/priority")]
        [Authorize(Policy = Permissions.TasksUpdate)]
        public async Task<IActionResult> ChangePriority(long id, [FromBody] ChangeTaskPriorityDto dto, CancellationToken cancellationToken)
        {
            // FIX: was missing cancellationToken (had a default value so it still compiled,
            // but was inconsistent with every other action here passing it explicitly).
            var result = await _taskService.ChangePriorityAsync(id, dto, CurrentUserId, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Tasks);

            return Ok(result);
        }
    }
}
