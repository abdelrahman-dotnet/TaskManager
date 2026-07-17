using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.API.Authorization;
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
    [Authorize(Policy = Permissions.AuditLogsView)]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<AuditLogController> _logger;

        public AuditLogController(IAuditLogService auditLogService, ICacheService cacheService, ILogger<AuditLogController> logger)
        {
            _auditLogService = auditLogService;
            _cacheService = cacheService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] AuditLogQueryParams q, CancellationToken cancellationToken)
        {
            var version = await _cacheService.GetVersionAsync(CacheDomains.AuditLogs);
            var cacheKey = CachKeyHelper.GenerateKey(CachePrefixes.AuditLogsList, version, q);

            var cached = await _cacheService.GetAsync<PagedResult<AuditLogReadDto>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Audit logs cache hit. CacheKey: {CacheKey}", cacheKey);
                return Ok(cached);
            }

            _logger.LogInformation("Audit logs cache miss. CacheKey: {CacheKey}", cacheKey);
            var result = await _auditLogService.GetAllAsync(q, cancellationToken);
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return Ok(result);
        }
    }
}