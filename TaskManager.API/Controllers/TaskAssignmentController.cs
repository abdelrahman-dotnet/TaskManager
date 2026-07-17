using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.API.Authorization;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.TaskAssignment;
using TaskManager.API.Helpers;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Bussiness.Caching;
using TaskManager.Bussiness.Services;

namespace TaskManager.API.Controllers
{
    // Assign/Unassign actions live under TaskController (POST/DELETE api/task/{id}/assign).
    // This controller only exposes read/listing for admin & reporting views.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = Permissions.TaskAssignmentsView)]
    public class TaskAssignmentController : ControllerBase
    {
        private readonly ITaskAssignmentService _taskAssignmentService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<TaskAssignmentController> _logger;

        public TaskAssignmentController(ITaskAssignmentService taskAssignmentService, ICacheService cacheService, ILogger<TaskAssignmentController> logger)
        {
            _taskAssignmentService = taskAssignmentService;
            _cacheService = cacheService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] TaskAssignmentQueryParams q, CancellationToken cancellationToken)
        {
            var version = await _cacheService.GetVersionAsync(CacheDomains.TaskAssignments);
            var cacheKey = CachKeyHelper.GenerateKey(CachePrefixes.TaskAssignmentsList, version, q);

            var cached = await _cacheService.GetAsync<PagedResult<TaskAssignmentReadDto>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Task assignments cache hit. CacheKey: {CacheKey}",cacheKey); return Ok(cached);
            }

            _logger.LogInformation("Task assignments cache miss. CacheKey: {CacheKey}", cacheKey);
            var result = await _taskAssignmentService.GetAllAsync(q, cancellationToken);
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return Ok(result);
        }
    }
}