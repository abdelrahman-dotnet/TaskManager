using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.API.Authorization;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.Team;
using TaskManager.API.Helpers;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Bussiness.Caching;
using TaskManager.Bussiness.Services;

namespace TaskManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TeamController : ControllerBase
    {
        private readonly ITeamService _teamService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<TeamController> _logger;
        private readonly ICurrentUserService _currentUser;

        public TeamController(ITeamService teamService, ICacheService cacheService, ILogger<TeamController> logger, ICurrentUserService currentUser)
        {
            _teamService = teamService;
            _cacheService = cacheService;
            _logger = logger;
            _currentUser = currentUser;
        }

        private string CurrentUserId => _currentUser.UserId!;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] TeamQueryParams q, CancellationToken cancellationToken)
        {
            var version = await _cacheService.GetVersionAsync(CacheDomains.Teams);
            // MEMBERSHIP: results now depend on who's asking - CurrentUserId must be part of
            // the cache key, same reasoning as TaskController.GetAll.
            var cacheKey = CachKeyHelper.GenerateKey(CachePrefixes.TeamsList, version, new { q, CurrentUserId });

            var cached = await _cacheService.GetAsync<PagedResult<TeamReadDto>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Teams cache hit. CacheKey: {CacheKey}", cacheKey);
                return Ok(cached);
            }

            _logger.LogInformation("Teams cache miss. CacheKey: {CacheKey}", cacheKey);
            var result = await _teamService.GetAllAsync(q, CurrentUserId, cancellationToken);
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            var version = await _cacheService.GetVersionAsync(CacheDomains.Teams);
            var cacheKey = CachKeyHelper.GenerateKey(CachePrefixes.TeamById, version, new { id, CurrentUserId });

            var cached = await _cacheService.GetAsync<TeamReadDto>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Team cache hit. TeamId: {TeamId}", id);
                return Ok(cached);
            }

            _logger.LogInformation("Team cache miss. TeamId: {TeamId}", id);
            var team = await _teamService.GetByIdAsync(id, CurrentUserId, cancellationToken);
            await _cacheService.SetAsync(cacheKey, team, TimeSpan.FromMinutes(5));

            return Ok(team);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.TeamsCreate)]
        public async Task<IActionResult> Create([FromBody] TeamCreateDto dto, CancellationToken cancellationToken)
        {
            var created = await _teamService.CreateAsync(dto, CurrentUserId, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Teams);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = Permissions.TeamsUpdate)]
        public async Task<IActionResult> Update(long id, [FromBody] TeamUpdateDto dto, CancellationToken cancellationToken)
        {
            var updated = await _teamService.UpdateAsync(id, dto, CurrentUserId, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Teams);

            return Ok(updated);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = Permissions.TeamsDelete)]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            await _teamService.DeleteAsync(id, CurrentUserId, cancellationToken);
            await _cacheService.IncrementVersionAsync(CacheDomains.Teams);

            return NoContent();
        }
    }
}
