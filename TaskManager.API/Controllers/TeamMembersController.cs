using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.API.Authorization;
using TaskManager.API.DTOs.TeamMember;
using TaskManager.API.Helpers;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Bussiness.Caching;
using TaskManager.Bussiness.Services;

namespace TaskManager.API.Controllers
{
    // Separate from TeamController on purpose - Team CRUD and Team Membership are different
    // responsibilities (per the project's Controller Standard: thin, single-purpose Controllers).
    [Route("api/teams/{teamId}/members")]
    [ApiController]
    [Authorize]
    public class TeamMembersController : ControllerBase
    {
        private readonly IMembershipService _membershipService;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly ILogger<TeamMembersController> _logger;
        private readonly ICurrentUserService _currentUser;

        public TeamMembersController(
            IMembershipService membershipService,
            IMapper mapper,
            ICacheService cacheService,
            ILogger<TeamMembersController> logger,
            ICurrentUserService currentUser)
        {
            _membershipService = membershipService;
            _mapper = mapper;
            _cacheService = cacheService;
            _logger = logger;
            _currentUser = currentUser;
        }

        private string CurrentUserId => _currentUser.UserId!;

        // FIX: no longer calls CanAccessTeamAsync here first - that Business Rule now lives 
        // entirely inside MembershipService.GetTeamMembersAsync (throws ForbiddenException
        // itself). This action is now purely Request -> Service -> Response.
        [HttpGet]
        public async Task<IActionResult> GetMembers(long teamId, CancellationToken cancellationToken)
        {
            var version = await _cacheService.GetVersionAsync(CacheDomains.Members);
            var cacheKey = CachKeyHelper.GenerateKey(CachePrefixes.MembersByTeam, version, new { teamId, CurrentUserId });
            var cached = await _cacheService.GetAsync<IEnumerable<TeamMemberReadDto>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("TeamMembers cache hit. CacheKey: {CacheKey}", cacheKey);
                return Ok(cached);
            }
            _logger.LogInformation("TeamMembers cache miss. CacheKey: {CacheKey}", cacheKey);
            var members = await _membershipService.GetTeamMembersAsync(teamId, CurrentUserId, cancellationToken);
            var result = _mapper.Map<IEnumerable<TeamMemberReadDto>>(members);
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return Ok(result);
        }

        // Permission (TeamsManageMembers) && Membership (Owner/Manager of THIS team, enforced
        // inside MembershipService.AddTeamMemberAsync via EnsureCanManageTeamAsync).
        [HttpPost]
        [Authorize(Policy = Permissions.TeamsManageMembers)]
        public async Task<IActionResult> AddMember(long teamId, [FromBody] AddTeamMemberDto dto, CancellationToken cancellationToken)
        {
            await _membershipService.AddTeamMemberAsync(teamId, dto.UserId, dto.Role, CurrentUserId, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Teams);
            await _cacheService.IncrementVersionAsync(CacheDomains.Members);

            _logger.LogInformation("Team member added via API. TeamId: {TeamId}, UserId: {UserId}, By: {CurrentUserId}", teamId, dto.UserId, CurrentUserId);
            return NoContent();
        }

        [HttpDelete("{userId}")]
        [Authorize(Policy = Permissions.TeamsManageMembers)]
        public async Task<IActionResult> RemoveMember(long teamId, string userId, CancellationToken cancellationToken)
        {
            await _membershipService.RemoveTeamMemberAsync(teamId, userId, CurrentUserId, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Teams);
            await _cacheService.IncrementVersionAsync(CacheDomains.Members);

            _logger.LogInformation("Team member removed via API. TeamId: {TeamId}, UserId: {UserId}, By: {CurrentUserId}", teamId, userId, CurrentUserId);
            return NoContent();
        }
    }
}
