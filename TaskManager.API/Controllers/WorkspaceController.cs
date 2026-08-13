using TaskManager.Bussiness.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.API.Authorization;
using TaskManager.API.DTOs.Workspace;
using TaskManager.Business.Services.Interfaces;

namespace TaskManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WorkspaceController : ControllerBase
    {
        private readonly IWorkspaceService _workspaceService;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<WorkspaceController> _logger;

        public WorkspaceController(
            IWorkspaceService workspaceService,
            ICurrentUserService currentUser,
            ILogger<WorkspaceController> logger)
        {
            _workspaceService = workspaceService;
            _currentUser = currentUser;
            _logger = logger;
        }

        // POST /api/workspaces
        // Creates a workspace and makes the caller its Owner (Membership System).
        [HttpPost]
        [Authorize(Policy = Permissions.WorkspacesCreate)]
        [ProducesResponseType(typeof(WorkspaceReadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] WorkspaceCreateDto dto, CancellationToken cancellationToken)
        {
            var workspaceId = await _workspaceService.CreateWorkspaceAsync(dto, _currentUser.UserId, cancellationToken);

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
        [Authorize(Policy = Permissions.WorkspacesView)]
        [ProducesResponseType(typeof(IEnumerable<WorkspaceReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
        {
            var result = await _workspaceService.GetMyWorkspacesAsync(_currentUser.UserId, cancellationToken);
            return Ok(result);
        }
    }
}