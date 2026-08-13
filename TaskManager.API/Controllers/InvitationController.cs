using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using TaskManager.API.Authorization;
using TaskManager.API.DTOs.Invitation;
using TaskManager.Business.Services.Interfaces;

namespace TaskManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InvitationController : ControllerBase
    {
        private readonly IInvitationService _invitationService;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<InvitationController> _logger;

        public InvitationController(
            IInvitationService invitationService,
            ICurrentUserService currentUser,
            ILogger<InvitationController> logger)
        {
            _invitationService = invitationService;
            _currentUser = currentUser;
            _logger = logger;
        }

        // POST /api/invitation
        // Owner/Admin (MembersInvite). BR-INV-01 enforced in the Service:
        // role must be Admin/Member (never Owner), mandatory future expiry,
        // no duplicate Pending invitation for the same user + workspace.
        [HttpPost]
        [Authorize(Policy = Permissions.MembersInvite)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] InvitationCreateDto dto, CancellationToken cancellationToken)
        {
            var id = await _invitationService.SendInvitationAsync(dto, _currentUser.UserId, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id }, new { id });
        }

        // POST /api/invitation/resend
        // Owner/Admin (InvitationsResend) — re-invites a previously-removed member
        // (D-26). Always issued with the Member role.
        [HttpPost("resend")]
        [Authorize(Policy = Permissions.InvitationsResend)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Resend([FromBody] InvitationResendDto dto, CancellationToken cancellationToken)
        {
            var id = await _invitationService.ResendInvitationAsync(dto, _currentUser.UserId, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id }, new { id });
        }

        // POST /api/invitation/{id}/revoke
        // Owner/Admin (InvitationsCancel) — any active Owner/Admin of the workspace
        // may cancel a pending invitation (D-27), not just the sender.
        [HttpPost("{id:long}/revoke")]
        [Authorize(Policy = Permissions.InvitationsCancel)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Revoke(long id, CancellationToken cancellationToken)
        {
            await _invitationService.RevokeInvitationAsync(id, _currentUser.UserId, cancellationToken);
            return NoContent();
        }

        // GET /api/invitation?workspaceId=...
        // Owner/Admin (InvitationsView) — lists invitations for a workspace.
        [HttpGet]
        [Authorize(Policy = Permissions.InvitationsView)]
        [ProducesResponseType(typeof(IEnumerable<InvitationReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> List([FromQuery] long workspaceId, CancellationToken cancellationToken)
        {
            var result = await _invitationService.GetInvitationsAsync(workspaceId, _currentUser.UserId, cancellationToken);
            return Ok(result);
        }

        // GET /api/invitation/{id}?workspaceId=...
        // Owner/Admin (InvitationsView) — a single invitation by id, scoped to a
        // workspace the caller can view (pipeline runs in GetInvitationByIdAsync).
        [HttpGet("{id:long}")]
        [Authorize(Policy = Permissions.InvitationsView)]
        [ProducesResponseType(typeof(InvitationReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(long id, [FromQuery] long workspaceId, CancellationToken cancellationToken)
        {
            var invitation = await _invitationService.GetInvitationByIdAsync(id, workspaceId, _currentUser.UserId, cancellationToken);
            return Ok(invitation);
        }

        // POST /api/invitation/{id}/accept
        // Self-service: no policy. BR-INV-02 enforced in the Service
        // (Pending, not expired, addressed to CurrentUser, workspace Active).
        [HttpPost("{id:long}/accept")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Accept(long id, CancellationToken cancellationToken)
        {
            await _invitationService.AcceptInvitationAsync(id, _currentUser.UserId, cancellationToken);
            return NoContent();
        }

        // POST /api/invitation/{id}/reject
        // Self-service: no policy. BR-INV-02 enforced in the Service.
        [HttpPost("{id:long}/reject")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Reject(long id, CancellationToken cancellationToken)
        {
            await _invitationService.RejectInvitationAsync(id, _currentUser.UserId, cancellationToken);
            return NoContent();
        }
    }
}
