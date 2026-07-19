using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.API.Authorization;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.Project;
using TaskManager.API.Helpers;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Bussiness.Caching;
using TaskManager.Bussiness.Services;

namespace TaskManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<ProjectController> _logger;
        private readonly ICurrentUserService _currentUser;

        public ProjectController(IProjectService projectService, ICacheService cacheService, ILogger<ProjectController> logger, ICurrentUserService currentUser)
        {
            _projectService = projectService;
            _cacheService = cacheService;
            _logger = logger;
            _currentUser = currentUser;
        }

        private string CurrentUserId => _currentUser.UserId!;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ProjectQueryParams q, CancellationToken cancellationToken)
        {
            var version = await _cacheService.GetVersionAsync(CacheDomains.Projects);
            // MEMBERSHIP: results now depend on who's asking - CurrentUserId must be part of
            // the cache key, same reasoning as TaskController.GetAll.
            var cacheKey = CachKeyHelper.GenerateKey(CachePrefixes.ProjectsList, version, new { q, CurrentUserId });

            var cached = await _cacheService.GetAsync<PagedResult<ProjectReadDto>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Projects cache hit. CacheKey: {CacheKey}", cacheKey);
                return Ok(cached);
            }

            _logger.LogInformation("Projects cache miss. CacheKey: {CacheKey}", cacheKey);
            var result = await _projectService.GetAllAsync(q, CurrentUserId, cancellationToken);
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            var version = await _cacheService.GetVersionAsync(CacheDomains.Projects);
            var cacheKey = CachKeyHelper.GenerateKey(CachePrefixes.ProjectById, version, new { id, CurrentUserId });

            var cached = await _cacheService.GetAsync<ProjectDetailsReadDto>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Project cache hit. ProjectId: {ProjectId}", id);
                return Ok(cached);
            }

            _logger.LogInformation("Project cache miss. ProjectId: {ProjectId}", id);
            var project = await _projectService.GetByIdAsync(id, CurrentUserId, cancellationToken);
            await _cacheService.SetAsync(cacheKey, project, TimeSpan.FromMinutes(5));

            return Ok(project);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.ProjectsCreate)]
        public async Task<IActionResult> Create([FromBody] ProjectCreateDto dto, CancellationToken cancellationToken)
        {
            var created = await _projectService.CreateAsync(dto, CurrentUserId, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Projects);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = Permissions.ProjectsUpdate)]
        public async Task<IActionResult> Update(long id, [FromBody] ProjectUpdateDto dto, CancellationToken cancellationToken)
        {
            var updated = await _projectService.UpdateAsync(id, dto, CurrentUserId, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Projects);

            return Ok(updated);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = Permissions.ProjectsDelete)]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            await _projectService.DeleteAsync(id, CurrentUserId, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Projects);

            return NoContent();
        }
    }
}
