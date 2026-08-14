using TaskManager.API.Exceptions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TaskManager.API.Config;
using TaskManager.API.Config.FiltersConfigs;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.Project;
using TaskManager.API.Extentions;
using TaskManager.API.Helpers;
using TaskManager.Bussiness.Authorization;
using TaskManager.Bussiness.Authorization.ResourceConditions;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Business.UnitOfWork;
using TaskManager.Data.Entities;

namespace TaskManager.API.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ProjectService> _logger;
        private readonly IAuditLogService _auditLogService;
        private readonly IMembershipService _membershipService;
        private readonly IWorkspaceAuthorizationService _authService;

        public ProjectService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<ProjectService> logger,
            IAuditLogService auditLogService,
            IMembershipService membershipService,
            IWorkspaceAuthorizationService authService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _auditLogService = auditLogService;
            _membershipService = membershipService;
            _authService = authService;
        }

        // RESOLVE: Project -> Workspace for the Authorization Pipeline (Visibility).
        private async Task<long> ResolveProjectWorkspaceAsync(long projectId, CancellationToken cancellationToken)
        {
            var workspaceId = await _unitOfWork.Projects.GetAllQuery()
                .Where(p => p.Id == projectId)
                .Select(p => (long?)p.WorkspaceId)
                .FirstOrDefaultAsync(cancellationToken);

            if (workspaceId == null)
            {
                _logger.LogWarning("Project not found or has no workspace. ProjectId: {ProjectId}", projectId);
                throw new NotFoundException("Project not found.");
            }

            return workspaceId.Value;
        }

        // PIPELINE FAILURE HANDLING: Visibility (NotFound) -> 404, everything else -> 403.
        // Same shape used by TaskService/CommentService/AttachmentService/NotificationService.
        private void ThrowIfFailed(AuthorizationResult authResult, long resourceId, string userId, string op)
        {
            _logger.LogWarning(
                "{Op} pipeline failed. Reason: {Reason}, UserId: {UserId}, ProjectId: {ProjectId}",
                op, authResult.FailureReason, userId, resourceId);

            throw authResult.FailureReason == AuthorizationFailureReason.NotFound
                ? new NotFoundException(authResult.Message)
                : new ForbiddenException(authResult.Message);
        }

        // VISIBILITY (read-listing): filters to projects the user belongs to, as an IN
        // subquery against ProjectMembers. No single workspaceId for a cross-workspace
        // list, so read-listing keeps the membership-scoped query rather than the
        // per-workspace pipeline.
        public async Task<PagedResult<ProjectReadDto>> GetAllAsync(ProjectQueryParams queryParams, string currentUserId, CancellationToken cancellationToken = default)
        {
            var query = _unitOfWork.Projects.GetAllQuery().AsNoTracking();

            var memberProjectIds = _unitOfWork.ProjectMembers.GetAllQuery()
                .Where(pm => pm.WorkspaceMember.UserId == currentUserId)
                .Select(pm => pm.ProjectId);

            query = query.Where(p => memberProjectIds.Contains(p.Id));

            query = query.ApplyFiltering(queryParams, ProjectFilterConfig.map);

            if (!string.IsNullOrWhiteSpace(queryParams.Search))
            {
                var search = queryParams.Search;
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    p.Description != null && p.Description.Contains(search));
            }

            query = query.ApplySorting(queryParams.Sorts, AllowedSortingFields.Projects, x => x.Id);

            var projected = query.ProjectTo<ProjectReadDto>(_mapper.ConfigurationProvider);
            var result = await projected.ToPagedResultAsync(queryParams.Page, queryParams.PageSize, cancellationToken);

            _logger.LogInformation("Projects retrieved successfully. Count: {Count}", result.Data.Count);
            return result;
        }

        public async Task<ProjectDetailsReadDto> GetByIdAsync(long id, string currentUserId, CancellationToken cancellationToken = default)
        {
            var project = await _unitOfWork.Projects.GetDetailsAsync(id, cancellationToken);
            if (project == null)
            {
                _logger.LogWarning("GetProjectById failed. Project not found. ProjectId: {ProjectId}", id);
                throw new NotFoundException("Project not found.");
            }

            // PROJECT PIVOT: Project -> Workspace for pipeline stage 1 (Visibility).
            var workspaceId = await ResolveProjectWorkspaceAsync(id, cancellationToken);

            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.WorkspaceView);

            if (!authResult.Succeeded)
                ThrowIfFailed(authResult, id, currentUserId, "GetProjectById");

            return _mapper.Map<ProjectDetailsReadDto>(project);
        }

        // PIPELINE (Auth Pipeline): Visibility -> Permission (Projects.Create) ->
        // Operation. The controller passes the target WorkspaceId (create DTOs carry
        // none - the route is POST /api/projects/{workspaceId}).
        public async Task<ProjectReadDto> CreateAsync(ProjectCreateDto dto, long workspaceId, string currentUserId, CancellationToken cancellationToken = default)
        {
            // FK validation before save (Validation step) - every requested Team must exist.
            foreach (var teamId in dto.TeamIds)
            {
                var teamExists = await _unitOfWork.Teams.ExistsAsync(t => t.Id == teamId, cancellationToken);
                if (!teamExists)
                    throw new NotFoundException("One of the specified teams was not found.");
            }

            // Business Validation.
            if (dto.StartDate.HasValue && dto.EndDate.HasValue && dto.EndDate < dto.StartDate)
                throw new BadRequestException("End date cannot be before start date.");

            // Authorization Pipeline (Visibility -> Permission: Projects.Create) - BEFORE
            // any persistence. Workspace is already validated by the pipeline itself.
            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.ProjectsCreate);

            if (!authResult.Succeeded)
                ThrowIfFailed(authResult, workspaceId, currentUserId, "CreateProject");

            // Mapping + explicit WorkspaceId (DTOs carry none; setting it here also fixes
            // the legacy WorkspaceId=0 FK bug).
            var project = _mapper.Map<Project>(dto);
            project.WorkspaceId = workspaceId;
            project.CreatedByUserId = currentUserId;

            // RESOLVE + attach the requested Team links (M:N junction) BEFORE the first save
            // so EF assigns ProjectTeams.ProjectId automatically.
            if (dto.TeamIds != null && dto.TeamIds.Count > 0)
            {
                foreach (var teamId in dto.TeamIds)
                {
                    project.ProjectTeams.Add(new ProjectTeam { TeamId = teamId });
                }
            }

            // Repository.
            await _unitOfWork.Projects.AddAsync(project, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            // MEMBERSHIP: same pattern as TeamService.CreateAsync - creating the first Owner
            // is this method's job, not IMembershipService.AddProjectMemberAsync's.
            var ownerWm = await _unitOfWork.WorkspaceMembers.GetAllQuery()
                .Where(wm => wm.UserId == currentUserId && wm.WorkspaceId == project.WorkspaceId)
                .FirstOrDefaultAsync(cancellationToken);
            if (ownerWm is null)
            {
                _logger.LogWarning("CreateProject failed. Caller is not a member of the workspace. UserId: {UserId}, WorkspaceId: {WorkspaceId}", currentUserId, project.WorkspaceId);
                throw new NotFoundException("You must be a member of this workspace to create a project.");
            }

            var ownerMembership = new ProjectMember
            {
                ProjectId = project.Id,
                WorkspaceMemberId = ownerWm.Id
            };
            await _unitOfWork.ProjectMembers.AddAsync(ownerMembership, cancellationToken);

            // Audit: Create is Save -> Audit -> Save, since project.Id only exists after the first save.
            var newValues = JsonSerializer.Serialize(new
            {
                project.Name,
                project.Description,
                project.StartDate,
                project.EndDate
            });
            await _auditLogService.LogAsync(currentUserId, "Create Project", nameof(Project), project.Id.ToString(), null, newValues);
            // Second save - persists the ProjectMember(Owner) and the audit row together.
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Project created successfully. ProjectId: {ProjectId},CurrentUserId:{UserId}", project.Id, currentUserId);
            return _mapper.Map<ProjectReadDto>(project);
        }

        public async Task<ProjectReadDto> UpdateAsync(long id, ProjectUpdateDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            // Load Entity + PROJECT PIVOT: Project -> Workspace.
            var project = await _unitOfWork.Projects.GetByIdAsync(id, cancellationToken);
            if (project == null)
            {
                _logger.LogWarning("UpdateProject failed. Project not found.ProjectId:{ProjectId}", id);
                throw new NotFoundException("Project not found.");
            }

            var workspaceId = project.WorkspaceId;

            // Authorization Pipeline: Visibility -> Permission (Projects.Update) ->
            // Condition: archived projects cannot be edited.
            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.ProjectsUpdate,
                new ProjectArchivedCondition(project));

            if (!authResult.Succeeded)
                ThrowIfFailed(authResult, id, currentUserId, "UpdateProject");

            // Business Validation.
            if (dto.StartDate.HasValue && dto.EndDate.HasValue && dto.EndDate < dto.StartDate)
                throw new BadRequestException("End date cannot be before start date.");

            var oldValues = JsonSerializer.Serialize(new
            {
                project.Name,
                project.Description,
                project.StartDate,
                project.EndDate,
                project.IsArchived
            });

            // Mapping.
            _mapper.Map(dto, project);
            project.UpdatedAt = DateTime.UtcNow;

            // Repository.
            _unitOfWork.Projects.Update(project);

            // Audit: Update is Audit -> Save, since the Id is already known.
            var newValues = JsonSerializer.Serialize(new
            {
                project.Name,
                project.Description,
                project.StartDate,
                project.EndDate,
                project.IsArchived
            });
            await _auditLogService.LogAsync(currentUserId, "Update Project", nameof(Project), id.ToString(), oldValues, newValues);

            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Project updated successfully. ProjectId: {ProjectId},CurrentUserId: {UserId}", id, currentUserId);
            return _mapper.Map<ProjectReadDto>(project);
        }

        public async Task DeleteAsync(long id, string currentUserId, CancellationToken cancellationToken = default)
        {
            // Load Entity + PROJECT PIVOT: Project -> Workspace.
            var project = await _unitOfWork.Projects.GetByIdAsync(id, cancellationToken);
            if (project == null)
            {
                _logger.LogWarning("DeleteProject failed. Project not found. ProjectId: {ProjectId}", id);
                throw new NotFoundException("Project not found.");
            }

            var workspaceId = project.WorkspaceId;

            // Authorization Pipeline: Visibility -> Permission (Projects.Delete) ->
            // Condition: archived projects cannot be deleted (restore first).
            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.ProjectsDelete,
                new ProjectArchivedCondition(project));

            if (!authResult.Succeeded)
                ThrowIfFailed(authResult, id, currentUserId, "DeleteProject");

            // Repository.
            _unitOfWork.Projects.Delete(project);

            // Audit: Delete is Audit -> Save, since the Id is already known.
            var oldValues = JsonSerializer.Serialize(new
            {
                project.Name,
                project.Description,
                project.StartDate,
                project.EndDate,
                project.IsArchived
            });
            await _auditLogService.LogAsync(currentUserId, "Delete Project", nameof(Project), id.ToString(), oldValues, null);

            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Project deleted successfully. ProjectId: {ProjectId},CurrentUserId: {UserId}", id, currentUserId);
        }

        // PIPELINE: Visibility -> Permission (Projects.Archive) -> Operation.
        // Soft archive (IsArchived=true) rather than deletion - tasks/comments keep history.
        public async Task<ProjectReadDto> ArchiveAsync(long id, string currentUserId, CancellationToken cancellationToken = default)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(id, cancellationToken);
            if (project == null)
            {
                _logger.LogWarning("ArchiveProject failed. Project not found. ProjectId: {ProjectId}", id);
                throw new NotFoundException("Project not found.");
            }

            var workspaceId = project.WorkspaceId;

            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.ProjectsArchive,
                new ProjectArchivedCondition(project));

            if (!authResult.Succeeded)
                ThrowIfFailed(authResult, id, currentUserId, "ArchiveProject");

            if (project.IsArchived)
            {
                _logger.LogWarning("ArchiveProject called on an already archived project. ProjectId: {ProjectId}", id);
                throw new BadRequestException("This project is already archived.");
            }

            var oldValues = JsonSerializer.Serialize(new { IsArchived = project.IsArchived });

            project.IsArchived = true;
            project.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Projects.Update(project);

            var newValues = JsonSerializer.Serialize(new { IsArchived = project.IsArchived });
            await _auditLogService.LogAsync(currentUserId, "Archive Project", nameof(Project), id.ToString(), oldValues, newValues);

            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Project archived. ProjectId: {ProjectId}, CurrentUserId: {UserId}", id, currentUserId);
            return _mapper.Map<ProjectReadDto>(project);
        }

        // PIPELINE: Visibility -> Permission (Projects.Update) -> Operation.
        // Restoring an archived project is an update - no separate permission exists in the Spec.
        public async Task<ProjectReadDto> RestoreAsync(long id, string currentUserId, CancellationToken cancellationToken = default)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(id, cancellationToken);
            if (project == null)
            {
                _logger.LogWarning("RestoreProject failed. Project not found. ProjectId: {ProjectId}", id);
                throw new NotFoundException("Project not found.");
            }

            var workspaceId = project.WorkspaceId;

            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.ProjectsUpdate);

            if (!authResult.Succeeded)
                ThrowIfFailed(authResult, id, currentUserId, "RestoreProject");

            if (!project.IsArchived)
            {
                _logger.LogWarning("RestoreProject called on a non-archived project. ProjectId: {ProjectId}", id);
                throw new BadRequestException("This project is not archived.");
            }

            var oldValues = JsonSerializer.Serialize(new { IsArchived = project.IsArchived });

            project.IsArchived = false;
            project.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Projects.Update(project);

            var newValues = JsonSerializer.Serialize(new { IsArchived = project.IsArchived });
            await _auditLogService.LogAsync(currentUserId, "Restore Project", nameof(Project), id.ToString(), oldValues, newValues);

            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Project restored. ProjectId: {ProjectId}, CurrentUserId: {UserId}", id, currentUserId);
            return _mapper.Map<ProjectReadDto>(project);
        }

        // PIPELINE: Visibility -> Permission (Projects.ManageTeams) -> BR (same workspace).
        public async Task AttachTeamAsync(long projectId, long teamId, string currentUserId, CancellationToken cancellationToken = default)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId, cancellationToken);
            if (project == null)
                throw new NotFoundException("Project not found.");

            var team = await _unitOfWork.Teams.GetByIdAsync(teamId, cancellationToken);
            if (team == null)
                throw new NotFoundException("Team not found.");

            if (team.WorkspaceId != project.WorkspaceId)
                throw new BadRequestException("The team belongs to a different workspace.");

            var authResult = await _authService.AuthorizeAsync(
                project.WorkspaceId,
                currentUserId,
                Permissions.ProjectsManageTeams);

            if (!authResult.Succeeded)
                ThrowIfFailed(authResult, projectId, currentUserId, "AttachTeam");

            var alreadyAttached = await _unitOfWork.ProjectTeams.ExistsAsync(
                pt => pt.ProjectId == projectId && pt.TeamId == teamId, cancellationToken);

            if (alreadyAttached)
                throw new ConflictException("This team is already attached to the project.");

            await _unitOfWork.ProjectTeams.AddAsync(new ProjectTeam { ProjectId = projectId, TeamId = teamId }, cancellationToken);
            await _auditLogService.LogAsync(currentUserId, "Attach Team to Project", nameof(ProjectTeam), $"P{projectId}-T{teamId}", null,
                JsonSerializer.Serialize(new { ProjectId = projectId, TeamId = teamId }));
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Team attached to project. ProjectId: {ProjectId}, TeamId: {TeamId}, CurrentUserId: {UserId}", projectId, teamId, currentUserId);
        }

        // PIPELINE: Visibility -> Permission (Projects.ManageTeams) -> BR (same workspace).
        public async Task DetachTeamAsync(long projectId, long teamId, string currentUserId, CancellationToken cancellationToken = default)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId, cancellationToken);
            if (project == null)
                throw new NotFoundException("Project not found.");

            var team = await _unitOfWork.Teams.GetByIdAsync(teamId, cancellationToken);
            if (team == null)
                throw new NotFoundException("Team not found.");

            var link = await _unitOfWork.ProjectTeams.GetAllQuery()
                .Where(pt => pt.ProjectId == projectId && pt.TeamId == teamId)
                .FirstOrDefaultAsync(cancellationToken);

            if (link == null)
                throw new NotFoundException("This team is not attached to the project.");

            var authResult = await _authService.AuthorizeAsync(
                project.WorkspaceId,
                currentUserId,
                Permissions.ProjectsManageTeams);

            if (!authResult.Succeeded)
                ThrowIfFailed(authResult, projectId, currentUserId, "DetachTeam");

            var oldValues = JsonSerializer.Serialize(new { ProjectId = projectId, TeamId = teamId });

            _unitOfWork.ProjectTeams.Delete(link);
            await _auditLogService.LogAsync(currentUserId, "Detach Team from Project", nameof(ProjectTeam), $"P{projectId}-T{teamId}", oldValues, null);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Team detached from project. ProjectId: {ProjectId}, TeamId: {TeamId}, CurrentUserId: {UserId}", projectId, teamId, currentUserId);
        }
    }
}
