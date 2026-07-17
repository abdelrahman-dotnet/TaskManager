using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.API.Authorization;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.TaskStatusHistory;
using TaskManager.API.Helpers;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Bussiness.Caching;
using TaskManager.Bussiness.Services;

namespace TaskManager.API.Controllers
{
    // Read-only. Entries are created internally by TaskController's ChangeStatus action -> TaskService.ChangeStatusAsync.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = Permissions.TaskStatusHistoryView)]
    public class TaskStatusHistoryController : ControllerBase
    {
        private readonly ITaskStatusHistoryService _taskStatusHistoryService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<TaskStatusHistoryController> _logger;

        public TaskStatusHistoryController(ITaskStatusHistoryService taskStatusHistoryService, ICacheService cacheService, ILogger<TaskStatusHistoryController> logger)
        {
            _taskStatusHistoryService = taskStatusHistoryService;
            _cacheService = cacheService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] TaskStatusHistoryQueryParams q, CancellationToken cancellationToken)
        {
            var version = await _cacheService.GetVersionAsync(CacheDomains.TaskStatusHistories);
            var cacheKey = CachKeyHelper.GenerateKey(CachePrefixes.TaskStatusHistoriesList, version, q);

            var cached = await _cacheService.GetAsync<PagedResult<TaskStatusHistoryReadDto>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Task status histories cache hit. CacheKey: {CacheKey}", cacheKey);
                return Ok(cached);
            }

            _logger.LogInformation("Task status histories cache miss. CacheKey: {CacheKey}", cacheKey);
            var result = await _taskStatusHistoryService.GetAllAsync(q, cancellationToken);
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return Ok(result);
        }
    }
}