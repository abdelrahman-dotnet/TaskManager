using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BizPermissions = TaskManager.Bussiness.Authorization.Permissions;

using TaskManager.API.DTOs.AuditLog;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.Helpers;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Bussiness.Caching;
using TaskManager.Bussiness.Services;

namespace TaskManager.API.Controllers
{
    // Read-only. Entries are written internally via IAuditLogService.LogAsync from other services.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = BizPermissions.AuditLogView)]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<AuditLogController> _logger;
        private readonly ICurrentUserService _currentUser;

        public AuditLogController(IAuditLogService auditLogService, ICacheService cacheService, ILogger<AuditLogController> logger, ICurrentUserService currentUser)
        {
            _auditLogService = auditLogService;
            _cacheService = cacheService;
            _logger = logger;
            _currentUser = currentUser;
        }

        private string CurrentUserId => _currentUser.UserId!;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] AuditLogQueryParams q, CancellationToken cancellationToken)
        {
            var version = await _cacheService.GetVersionAsync(CacheDomains.AuditLogs);
            var cacheKey = CachKeyHelper.GenerateKey(CachePrefixes.AuditLogsList, version, new { q, CurrentUserId });

            var cached = await _cacheService.GetAsync<PagedResult<AuditLogReadDto>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Audit logs cache hit. CacheKey: {CacheKey}", cacheKey);
                return Ok(cached);
            }

            _logger.LogInformation("Audit logs cache miss. CacheKey: {CacheKey}", cacheKey);
            var result = await _auditLogService.GetAllAsync(q, CurrentUserId, cancellationToken);
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return Ok(result);
        }
    }
}
