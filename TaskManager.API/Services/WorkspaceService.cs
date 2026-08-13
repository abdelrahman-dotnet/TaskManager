using Microsoft.EntityFrameworkCore;
using TaskManager.Business.UnitOfWork;
using TaskManager.Bussiness.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;
using TaskManager.API.DTOs.Workspace;
using TaskManager.API.Exceptions;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Data.Entities;
using TaskManager.Data.Enums;

namespace TaskManager.API.Services
{
    public class WorkspaceService : IWorkspaceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<WorkspaceService> _logger;

        public WorkspaceService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            IAuditLogService auditLogService,
            ILogger<WorkspaceService> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        // â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private static string ToSlug(string name)
        {
            var slug = Regex.Replace(name.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-");
            slug = slug.Trim('-');
            return string.IsNullOrEmpty(slug) ? "workspace" : slug;
        }

        private async Task<string> GetUniqueSlugAsync(string baseSlug, CancellationToken cancellationToken)
        {
            var slug = baseSlug;
            var suffix = 1;
            while (await _unitOfWork.Workspaces.GetAllQuery()
                .AnyAsync(w => w.Slug == slug, cancellationToken))
            {
                slug = $"{baseSlug}-{suffix++}";
            }
            return slug;
        }

        // â”€â”€ Create â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public async Task<long> CreateWorkspaceAsync(WorkspaceCreateDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(currentUserId);
            if (user is null)
                throw new BadRequestException("Current user could not be resolved.");

            var slug = await GetUniqueSlugAsync(ToSlug(dto.Name), cancellationToken);

            var workspace = new Workspace
            {
                Name = dto.Name.Trim(),
                Slug = slug,
                Description = dto.Description?.Trim(),
                Status = WorkspaceStatus.Active
            };

            await _unitOfWork.Workspaces.AddAsync(workspace, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            // The creator automatically becomes the workspace Owner â€” no one else
            // can ever seed this relationship.
            var ownerMember = new WorkspaceMember
            {
                WorkspaceId = workspace.Id,
                UserId = currentUserId,
                Role = WorkspaceRole.Owner,
                Status = WorkspaceMemberStatus.Active
            };

            await _unitOfWork.WorkspaceMembers.AddAsync(ownerMember, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            var newValues = JsonSerializer.Serialize(new { Name = workspace.Name, Slug = workspace.Slug, OwnerUserId = currentUserId });
            await _auditLogService.LogAsync(currentUserId, "Create Workspace", nameof(Workspace), workspace.Id.ToString(), null, newValues, cancellationToken);
            await _auditLogService.LogAsync(currentUserId, "Join Workspace as Owner", nameof(WorkspaceMember), ownerMember.Id.ToString(), null, JsonSerializer.Serialize(new { WorkspaceId = workspace.Id, Role = WorkspaceRole.Owner }), cancellationToken);

            _logger.LogInformation("Workspace created. Id: {Id}, Name: {Name}, OwnerUserId: {UserId}", workspace.Id, workspace.Name, currentUserId);

            return workspace.Id;
        }

        // â”€â”€ List (own memberships) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public async Task<IEnumerable<WorkspaceReadDto>> GetMyWorkspacesAsync(string currentUserId, CancellationToken cancellationToken = default)
        {
            var memberships = await _unitOfWork.WorkspaceMembers.GetAllQuery()
                .AsNoTracking()
                .Where(wm => wm.UserId == currentUserId)
                .Select(wm => new
                {
                    wm.Id,
                    Workspace = wm.Workspace,
                    wm.Role
                })
                .ToListAsync(cancellationToken);

            return memberships.Select(m => new WorkspaceReadDto
            {
                Id = m.Workspace.Id,
                Name = m.Workspace.Name,
                Slug = m.Workspace.Slug,
                Description = m.Workspace.Description,
                LogoUrl = m.Workspace.LogoUrl,
                Status = m.Workspace.Status,
                Role = m.Role
            });
        }
    }
}