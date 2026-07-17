using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.API.Authorization;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.User;
using TaskManager.API.Helpers;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Bussiness.Caching;
using TaskManager.Bussiness.Services;

namespace TaskManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<UserController> _logger;
        private readonly ICurrentUserService _currentUser;

        public UserController(IUserService userService, ICacheService cacheService, ILogger<UserController> logger, ICurrentUserService currentUser)
        {
            _userService = userService;
            _cacheService = cacheService;
            _logger = logger;
            _currentUser = currentUser;
        }
        private string CurrentUserId => _currentUser.UserId!;
        private bool CanManageAny => _currentUser.HasPermission(Permissions.TasksManageAny);
        

        // GAP: no Permissions.UsersManageAny constant exists in Permissions.cs, even though the
        // standard lists "Users" under the Ownership/ManageAny pattern. Defaulting to false
        // (strict self-only enforcement) below until this permission is added - see notes at
        // the end of this response instead of inventing a constant that doesn't exist yet.
        private bool CanManageAnyUser => false;

        [HttpGet]
        [Authorize(Policy = Permissions.UsersView)]
        public async Task<IActionResult> GetAll([FromQuery] UserQueryParams q, CancellationToken cancellationToken)
        {
            var version = await _cacheService.GetVersionAsync(CacheDomains.Users);
            var cacheKey = CachKeyHelper.GenerateKey(CachePrefixes.UsersList, version, q);

            var cached = await _cacheService.GetAsync<PagedResult<UserReadDto>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Users cache hit. CacheKey: {CacheKey}", cacheKey);
                return Ok(cached);
            }

            _logger.LogInformation("Users cache miss. CacheKey: {CacheKey}", cacheKey);
            var result = await _userService.GetAllAsync(q, cancellationToken);
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = Permissions.UsersView)]
        public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
        {
            var version = await _cacheService.GetVersionAsync(CacheDomains.Users);
            var cacheKey = CachKeyHelper.GenerateKey(CachePrefixes.UserById, version, id);

            var cached = await _cacheService.GetAsync<UserReadDto>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("User cache hit. UserId: {UserId}", id);
                return Ok(cached);
            }

            _logger.LogInformation("User cache miss. UserId: {UserId}", id);
            var user = await _userService.GetByIdAsync(id, cancellationToken);
            await _cacheService.SetAsync(cacheKey, user, TimeSpan.FromMinutes(5));

            return Ok(user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UserUpdateDto dto, CancellationToken cancellationToken)
        {
            var updated = await _userService.UpdateAsync(id, dto, CurrentUserId, CanManageAnyUser, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Users);

            return Ok(updated);
        }

        [HttpPatch("{id}/status")]
        [Authorize(Policy = Permissions.UsersManageStatus)]
        public async Task<IActionResult> SetActiveStatus(string id, [FromBody] UserStatusDto dto, CancellationToken cancellationToken)
        {
            await _userService.SetActiveStatusAsync(id, dto, CurrentUserId, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Users);

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = Permissions.UsersDelete)]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            await _userService.DeleteAsync(id, CurrentUserId, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Users);

            return NoContent();
        }

        [HttpPost("{id}/roles")]
        [Authorize(Policy = Permissions.UsersManageRoles)]
        public async Task<IActionResult> AssignRole(string id, [FromBody] AssignRoleDto dto, CancellationToken cancellationToken)
        {
            await _userService.AssignRoleAsync(id, dto, CurrentUserId, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Users);

            return NoContent();
        }

        [HttpDelete("{id}/roles/{roleName}")]
        [Authorize(Policy = Permissions.UsersManageRoles)]
        public async Task<IActionResult> RemoveRole(string id, string roleName, CancellationToken cancellationToken)
        {
            await _userService.RemoveRoleAsync(id, roleName, CurrentUserId, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Users);

            return NoContent();
        }
    }
}