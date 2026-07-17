using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.API.Authorization;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.Role;
using TaskManager.API.Helpers;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Bussiness.Caching;
using TaskManager.Bussiness.Services;

namespace TaskManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = Permissions.RolesManage)]

    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<RoleController> _logger;
        private readonly ICurrentUserService _currentUser;
        public RoleController(IRoleService roleService, ICacheService cacheService, ILogger<RoleController> logger, ICurrentUserService currentUser)
        {
            _roleService = roleService;
            _cacheService = cacheService;
            _logger = logger;
            _currentUser = currentUser;
        }

        private string CurrentUserId => _currentUser.UserId!;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] RoleQueryParams q, CancellationToken cancellationToken)
        {
            var version = await _cacheService.GetVersionAsync(CacheDomains.Roles);
            var cacheKey = CachKeyHelper.GenerateKey(CachePrefixes.RolesList, version, q);

            var cached = await _cacheService.GetAsync<PagedResult<RoleReadDto>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Roles cache hit. CacheKey: {CacheKey}", cacheKey);
                return Ok(cached);
            }

            _logger.LogInformation("Roles cache miss. CacheKey: {CacheKey}", cacheKey);
            var result = await _roleService.GetAllAsync(q, cancellationToken);
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
        {
            var version = await _cacheService.GetVersionAsync(CacheDomains.Roles);
            var cacheKey = CachKeyHelper.GenerateKey(CachePrefixes.RoleById, version, id);

            var cached = await _cacheService.GetAsync<RoleReadDto>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Role cache hit. RoleId: {RoleId}", id);
                return Ok(cached);
            }

            _logger.LogInformation("Role cache miss. RoleId: {RoleId}", id);
            var role = await _roleService.GetByIdAsync(id, cancellationToken);
            await _cacheService.SetAsync(cacheKey, role, TimeSpan.FromMinutes(5));

            return Ok(role);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RoleCreateAndUpdateDto dto, CancellationToken cancellationToken)
        {
            var created = await _roleService.CreateAsync(dto, CurrentUserId, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Roles);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] RoleCreateAndUpdateDto dto, CancellationToken cancellationToken)
        {
            var updated = await _roleService.UpdateAsync(id, dto, CurrentUserId, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Roles);

            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            await _roleService.DeleteAsync(id, CurrentUserId, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Roles);

            return NoContent();
        }
    }
}