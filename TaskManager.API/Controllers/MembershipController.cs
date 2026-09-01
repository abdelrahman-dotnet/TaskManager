using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using TaskManager.API.Authorization;
using TaskManager.API.Constants;
using TaskManager.API.Helpers;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Bussiness.Caching;
using TaskManager.Bussiness.Services;
using TaskManager.Data.Enums;

namespace TaskManager.API.Controllers
{
    // Membership lifecycle for a workspace's members.
    // Authorization runs in IMembershipService (pipeline), so controller policies
    // stay broad (membership-gated) and the service owns the fine-grained checks.
    [Route("api/workspaces/{workspaceId}/members")]
    [ApiController]
    [EnableRateLimiting(RateLimitPolicyNames.Global)]

    public class MembershipController : ControllerBase
    {
        private readonly IMembershipService _membershipService;
        private readonly ICacheService _cacheService;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<MembershipController> _logger;

        public MembershipController(
            IMembershipService membershipService,
            ICacheService cacheService,
            ICurrentUserService currentUser,
            ILogger<MembershipController> logger)
        {
            _membershipService = membershipService;
            _cacheService = cacheService;
            _currentUser = currentUser;
            _logger = logger;
        }

        // PIPELINE (in service): Visibility -> Permission (WorkspaceView).
        [HttpGet]
        [Authorize(Policy = Permissions.WorkspacesView)]
        [ProducesResponseType(typeof(IEnumerable<WorkspaceMemberDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> List(long workspaceId, CancellationToken cancellationToken)
        {
            var version = await _cacheService.GetVersionAsync(CacheDomains.Members);
            var cacheKey = CachKeyHelper.GenerateKey(CachePrefixes.MembersList, version, new { workspaceId, CurrentUserId = _currentUser.UserId });
            var cached = await _cacheService.GetAsync<IEnumerable<WorkspaceMemberDto>>(cacheKey);
            if (cached != null)
            {
                _logger?.LogInformation("Members cache hit. CacheKey: {CacheKey}", cacheKey);
                return Ok(cached);
            }
            _logger?.LogInformation("Members cache miss. CacheKey: {CacheKey}", cacheKey);
            var members = await _membershipService.GetWorkspaceMembersAsync(workspaceId, _currentUser.UserId!, cancellationToken);
            var mapped = members.Select(m => new WorkspaceMemberDto
            {
                Id = m.Id,
                UserId = m.UserId,
                UserName = m.User?.UserName,
                Role = m.Role,
                Status = m.Status,
                JoinedAt = m.CreatedAt
            });
            await _cacheService.SetAsync(cacheKey, mapped, TimeSpan.FromMinutes(5));
            return Ok(mapped);
        }

        // PIPELINE (in service): Visibility -> Permission (Members.Remove) ->
        // Condition (Owner protected) -> BR-MEM-03 (assignment cleanup).
        [HttpDelete("{userId}")]
        [Authorize(Policy = Permissions.MembersRemove)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Remove(long workspaceId, string userId, CancellationToken cancellationToken)
        {
            await _membershipService.RemoveWorkspaceMemberAsync(workspaceId, userId, _currentUser.UserId!, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Projects);
            await _cacheService.IncrementVersionAsync(CacheDomains.Teams);
            await _cacheService.IncrementVersionAsync(CacheDomains.Members);

            return NoContent();
        }

        // PIPELINE (in service): Visibility -> Permission (Members.Suspend) -> BR (Owner protected).
        [HttpPut("{userId}/suspend")]
        [Authorize(Policy = Permissions.MembersSuspend)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Suspend(long workspaceId, string userId, CancellationToken cancellationToken)
        {
            await _membershipService.SuspendWorkspaceMemberAsync(workspaceId, userId, _currentUser.UserId!, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Projects);
            await _cacheService.IncrementVersionAsync(CacheDomains.Teams);
            await _cacheService.IncrementVersionAsync(CacheDomains.Members);

            return NoContent();
        }

        // PIPELINE (in service): Visibility -> Permission (Members.Suspend).
        [HttpPut("{userId}/unsuspend")]
        [Authorize(Policy = Permissions.MembersSuspend)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Unsuspend(long workspaceId, string userId, CancellationToken cancellationToken)
        {
            await _membershipService.UnsuspendWorkspaceMemberAsync(workspaceId, userId, _currentUser.UserId!, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Projects);
            await _cacheService.IncrementVersionAsync(CacheDomains.Teams);
            await _cacheService.IncrementVersionAsync(CacheDomains.Members);

            return NoContent();
        }
    }

    // Simple read DTO so the controller stays thin and the service stays agnostic of HTTP.
    public class WorkspaceMemberDto
    {
        public long Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public WorkspaceRole Role { get; set; }
        public WorkspaceMemberStatus Status { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
