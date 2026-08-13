using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.API.Authorization;
using TaskManager.API.DTOs.ProjectMember;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Bussiness.Caching;
using TaskManager.Bussiness.Services;

namespace TaskManager.API.Controllers
{
    // Separate from ProjectController on purpose - same reasoning as TeamMembersController.
    [Route("api/projects/{projectId}/members")]
    [ApiController]
    [Authorize]
    public class ProjectMembersController : ControllerBase
    {
        private readonly IMembershipService _membershipService;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly ILogger<ProjectMembersController> _logger;
        private readonly ICurrentUserService _currentUser;

        public ProjectMembersController(
            IMembershipService membershipService,
            IMapper mapper,
            ICacheService cacheService,
            ILogger<ProjectMembersController> logger,
            ICurrentUserService currentUser)
        {
            _membershipService = membershipService;
            _mapper = mapper;
            _cacheService = cacheService;
            _logger = logger;
            _currentUser = currentUser;
        }

        private string CurrentUserId => _currentUser.UserId!;

        // FIX: no longer calls CanAccessProjectAsync here first - that Business Rule now lives
        // entirely inside MembershipService.GetProjectMembersAsync (throws ForbiddenException
        // itself). This action is now purely Request -> Service -> Response.
        [HttpGet]
        public async Task<IActionResult> GetMembers(long projectId, CancellationToken cancellationToken)
        {
            var members = await _membershipService.GetProjectMembersAsync(projectId, CurrentUserId, cancellationToken);
            var result = _mapper.Map<IEnumerable<ProjectMemberReadDto>>(members);

            return Ok(result);
        }

        // Permission (ProjectsManageMembers) && Membership (Owner/Manager of THIS project,
        // enforced inside MembershipService.AddProjectMemberAsync via EnsureCanManageProjectAsync).
        [HttpPost]
        [Authorize(Policy = Permissions.ProjectsManageMembers)]
        public async Task<IActionResult> AddMember(long projectId, [FromBody] AddProjectMemberDto dto, CancellationToken cancellationToken)
        {
            await _membershipService.AddProjectMemberAsync(projectId, dto.UserId, dto.Role, CurrentUserId, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Projects);

            _logger.LogInformation("Project member added via API. ProjectId: {ProjectId}, UserId: {UserId}, By: {CurrentUserId}", projectId, dto.UserId, CurrentUserId);
            return NoContent();
        }

        [HttpDelete("{userId}")]
        [Authorize(Policy = Permissions.ProjectsManageMembers)]
        public async Task<IActionResult> RemoveMember(long projectId, string userId, CancellationToken cancellationToken)
        {
            await _membershipService.RemoveProjectMemberAsync(projectId, userId, CurrentUserId, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Projects);

            _logger.LogInformation("Project member removed via API. ProjectId: {ProjectId}, UserId: {UserId}, By: {CurrentUserId}", projectId, userId, CurrentUserId);
            return NoContent();
        }
    }
}
