using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskManager.API.Constants;
using TaskManager.API.DTOs.Workspace;
using TaskManager.API.Helpers;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Bussiness.Caching;
using TaskManager.Bussiness.Services;
using ApiPermissions = TaskManager.API.Authorization.Permissions;
using BizPermissions = TaskManager.Bussiness.Authorization.Permissions;

namespace TaskManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicyNames.Global)]

    public class WorkspaceController : ControllerBase
    {
        private readonly IWorkspaceService _workspaceService;
        private readonly ICacheService _cacheService;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<WorkspaceController> _logger;

        public WorkspaceController(
            IWorkspaceService workspaceService,
            ICacheService cacheService,
            ICurrentUserService currentUser,
            ILogger<WorkspaceController> logger)
        {
            _workspaceService = workspaceService;
            _cacheService = cacheService;
            _currentUser = currentUser;
            _logger = logger;
        }

        // POST /api/workspaces
        // Creates a workspace and makes the caller its Owner (Membership System).
        [HttpPost]
        [Authorize(Policy = ApiPermissions.WorkspacesCreate)]
        [ProducesResponseType(typeof(WorkspaceReadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] WorkspaceCreateDto dto, CancellationToken cancellationToken)
        {
            var workspaceId = await _workspaceService.CreateWorkspaceAsync(dto, _currentUser.UserId, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Workspaces);

            // Resolve the read DTO for the response (caller is guaranteed to be
            // the Owner at this point).
            var workspaces = (await _workspaceService.GetMyWorkspacesAsync(_currentUser.UserId, cancellationToken))
                .Where(w => w.Id == workspaceId)
                .ToList();

            return CreatedAtAction(nameof(GetMine), workspaces);
        }

        // GET /api/workspaces/mine
        // Lists the workspaces the caller belongs to. Membership is the gate â€”
        // every caller only sees their own memberships.
        [HttpGet("mine")]
        [Authorize(Policy = ApiPermissions.WorkspacesView)]
        [ProducesResponseType(typeof(IEnumerable<WorkspaceReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
        {
            var version = await _cacheService.GetVersionAsync(CacheDomains.Workspaces);
            var cacheKey = CachKeyHelper.GenerateKey(CachePrefixes.WorkspacesList, version, new { CurrentUserId = _currentUser.UserId });
            var cached = await _cacheService.GetAsync<IEnumerable<WorkspaceReadDto>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Workspaces cache hit. CacheKey: {CacheKey}", cacheKey);
                return Ok(cached);
            }
            _logger.LogInformation("Workspaces cache miss. CacheKey: {CacheKey}", cacheKey);
            var result = await _workspaceService.GetMyWorkspacesAsync(_currentUser.UserId, cancellationToken);
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            return Ok(result);
        }

        // PIPELINE: runs in the service (Visibility -> Permission: Workspace.Update).
        [HttpPut("{id}/suspend")]
        [Authorize(Policy = BizPermissions.WorkspaceUpdate)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Suspend(long id, CancellationToken cancellationToken)
        {
            await _workspaceService.SuspendWorkspaceAsync(id, _currentUser.UserId!, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Workspaces);
            return NoContent();
        }

        // PIPELINE: runs in the service (Visibility -> Permission: Workspace.Update).
        [HttpPut("{id}/activate")]
        [Authorize(Policy = BizPermissions.WorkspaceUpdate)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Activate(long id, CancellationToken cancellationToken)
        {
            await _workspaceService.ActivateWorkspaceAsync(id, _currentUser.UserId!, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Workspaces);
            return NoContent();
        }

        // PIPELINE: runs in the service (Visibility -> Permission: Workspace.Archive).
        [HttpPut("{id}/archive")]
        [Authorize(Policy = BizPermissions.WorkspaceArchive)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Archive(long id, CancellationToken cancellationToken)
        {
            await _workspaceService.ArchiveWorkspaceAsync(id, _currentUser.UserId!, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Workspaces);
            return NoContent();
        }

        // PIPELINE: runs in the service (Visibility -> Permission: Workspace.Update).
        [HttpPut("{id}/restore")]
        [Authorize(Policy = BizPermissions.WorkspaceUpdate)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Restore(long id, CancellationToken cancellationToken)
        {
            await _workspaceService.RestoreWorkspaceAsync(id, _currentUser.UserId!, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Workspaces);
            return NoContent();
        }

        // PIPELINE: runs in the service (Visibility -> Permission: Workspace.TransferOwnership
        // -> BR-WS-04). Body carries the target member's userId.
        [HttpPut("{id}/transfer")]
        [Authorize(Policy = BizPermissions.WorkspaceTransferOwnership)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> TransferOwnership(long id, [FromBody] WorkspaceTransferOwnershipDto dto, CancellationToken cancellationToken)
        {
            await _workspaceService.TransferOwnershipAsync(id, dto.TargetUserId, _currentUser.UserId!, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Workspaces);
            await _cacheService.IncrementVersionAsync(CacheDomains.Members);
            return NoContent();
        }
    }
}