using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.Notification;
using TaskManager.API.Helpers;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Bussiness.Caching;
using TaskManager.Bussiness.Services;

namespace TaskManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<NotificationController> _logger;
        private readonly ICurrentUserService _currentUser;

        public NotificationController(INotificationService notificationService, ICacheService cacheService, ILogger<NotificationController> logger, ICurrentUserService currentUser)
        {
            _notificationService = notificationService;
            _cacheService = cacheService;
            _logger = logger;
            _currentUser = currentUser;
        }

        private string CurrentUserId => _currentUser.UserId!;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] NotificationQueryParams q, CancellationToken cancellationToken)
        {
            var version = await _cacheService.GetVersionAsync(CacheDomains.Notifications);
            var cacheKey = CachKeyHelper.GenerateKey(CachePrefixes.NotificationsList, version, q);

            var cached = await _cacheService.GetAsync<PagedResult<NotificationReadDto>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Notifications cache hit. CacheKey: {CacheKey}", cacheKey);
                return Ok(cached);
            }

            _logger.LogInformation("Notifications cache miss. CacheKey: {CacheKey}", cacheKey);
            var result = await _notificationService.GetAllAsync(q, cancellationToken);
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return Ok(result);
        }

        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            await _notificationService.MarkAsReadAsync(id, CurrentUserId);
            await _cacheService.IncrementVersionAsync(CacheDomains.Notifications);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            await _notificationService.DeleteAsync(id, CurrentUserId);
            await _cacheService.IncrementVersionAsync(CacheDomains.Notifications);

            return NoContent();
        }
    }
}