using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.API.Authorization;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.TaskItemStatusHistory;
using TaskManager.API.Helpers;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Bussiness.Caching;
using TaskManager.Bussiness.Services;

namespace TaskManager.API.Controllers
{
    // Read-only. Entries are created internally by TaskController's ChangeStatus action -> TaskService.ChangeStatusAsync.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = Permissions.TaskItemStatusHistoryView)]
    public class TaskItemStatusHistoryController : ControllerBase
    {
        private readonly ITaskItemStatusHistoryService _TaskItemStatusHistoryService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<TaskItemStatusHistoryController> _logger;

        public TaskItemStatusHistoryController(ITaskItemStatusHistoryService TaskItemStatusHistoryService, ICacheService cacheService, ILogger<TaskItemStatusHistoryController> logger)
        {
            _TaskItemStatusHistoryService = TaskItemStatusHistoryService;
            _cacheService = cacheService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] TaskItemStatusHistoryQueryParams q, CancellationToken cancellationToken)
        {
            var version = await _cacheService.GetVersionAsync(CacheDomains.TaskItemStatusHistories);
            var cacheKey = CachKeyHelper.GenerateKey(CachePrefixes.TaskItemStatusHistoriesList, version, q);

            var cached = await _cacheService.GetAsync<PagedResult<TaskItemStatusHistoryReadDto>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Task status histories cache hit. CacheKey: {CacheKey}", cacheKey);
                return Ok(cached);
            }

            _logger.LogInformation("Task status histories cache miss. CacheKey: {CacheKey}", cacheKey);
            var result = await _TaskItemStatusHistoryService.GetAllAsync(q, cancellationToken);
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return Ok(result);
        }
    }
}
