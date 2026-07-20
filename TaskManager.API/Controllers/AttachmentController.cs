using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.API.Authorization;
using TaskManager.API.DTOs.Attachment;
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
    public class AttachmentController : ControllerBase
    {
        private readonly IAttachmentService _attachmentService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<AttachmentController> _logger;
        private readonly ICurrentUserService _currentUser;

        public AttachmentController(IAttachmentService attachmentService, ICacheService cacheService, ILogger<AttachmentController> logger, ICurrentUserService currentUser)
        {
            _attachmentService = attachmentService;
            _cacheService = cacheService;
            _logger = logger;
            _currentUser = currentUser;
        }

        private string CurrentUserId => _currentUser.UserId!;
        private bool CanManageAny => _currentUser.HasPermission(Permissions.AttachmentsManageAny);

        // No "Attachments.View" permission exists in Permissions.cs, so this stays under
        // the class-level plain [Authorize] - any authenticated user can list attachments
        // (Membership still filters which ones they actually see).
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] AttachmentQueryParams q, CancellationToken cancellationToken)
        {
            var version = await _cacheService.GetVersionAsync(CacheDomains.Attachments);
            var cacheKey = CachKeyHelper.GenerateKey(CachePrefixes.AttachmentsList, version, new { q, CurrentUserId, CanManageAny });

            var cached = await _cacheService.GetAsync<PagedResult<AttachmentReadDto>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Attachments cache hit. CacheKey: {CacheKey}", cacheKey);
                return Ok(cached);
            }

            _logger.LogInformation("Attachments cache miss. CacheKey: {CacheKey}", cacheKey);
            var result = await _attachmentService.GetAllAsync(q, CurrentUserId, CanManageAny, cancellationToken);
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.AttachmentsCreate)]
        public async Task<IActionResult> Create([FromBody] AttachmentCreateDto dto, CancellationToken cancellationToken)
        {
            // NOTE: the actual file bytes should be handled here (IFormFile, save to disk/blob storage)
            // before building AttachmentCreateDto - this endpoint currently only persists metadata.
            var created = await _attachmentService.CreateAsync(dto, CurrentUserId, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Attachments);

            return Ok(created);
        }

        // FIX: was [Authorize(Policy = Permissions.AttachmentsManageAny)], which meant only
        // ManageAny holders could reach this endpoint AT ALL - a plain user could never delete
        // even their own attachment, contradicting the Service's own ownership-check logic
        // (which assumed regular users would get in and be allowed to delete what they
        // uploaded). Relaxed to plain [Authorize] - Authentication only at the Controller; the
        // full decision (owner OR ManageAny) now lives entirely in AttachmentService.DeleteAsync.
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            await _attachmentService.DeleteAsync(id, CurrentUserId, CanManageAny, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Attachments);

            return NoContent();
        }
    }
}
