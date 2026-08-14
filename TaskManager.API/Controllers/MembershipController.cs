using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.API.Authorization;
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
    public class MembershipController : ControllerBase
    {
        private readonly IMembershipService _membershipService;
        private readonly ICacheService _cacheService;
        private readonly ICurrentUserService _currentUser;

        public MembershipController(
            IMembershipService membershipService,
            ICacheService cacheService,
            ICurrentUserService currentUser)
        {
            _membershipService = membershipService;
            _cacheService = cacheService;
            _currentUser = currentUser;
        }

        // PIPELINE (in service): Visibility -> Permission (WorkspaceView).
        [HttpGet]
        [Authorize(Policy = Permissions.WorkspacesView)]
        [ProducesResponseType(typeof(IEnumerable<WorkspaceMemberDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> List(long workspaceId, CancellationToken cancellationToken)
        {
            var members = await _membershipService.GetWorkspaceMembersAsync(workspaceId, _currentUser.UserId!, cancellationToken);

            return Ok(members.Select(m => new WorkspaceMemberDto
            {
                Id = m.Id,
                UserId = m.UserId,
                UserName = m.User?.UserName,
                Role = m.Role,
                Status = m.Status,
                JoinedAt = m.CreatedAt
            }));
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
