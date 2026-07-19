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

        public ProjectService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<ProjectService> logger,
            IAuditLogService auditLogService,
            IMembershipService membershipService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _auditLogService = auditLogService;
            _membershipService = membershipService;
        }

        // MEMBERSHIP: filters to projects the user belongs to, as an IN subquery against
        // ProjectMembers. No bypass - see IProjectService.cs's comment (no Projects.ManageAny
        // exists yet).
        public async Task<PagedResult<ProjectReadDto>> GetAllAsync(ProjectQueryParams queryParams, string currentUserId, CancellationToken cancellationToken = default)
        {
            var query = _unitOfWork.Projects.GetAllQuery().AsNoTracking();

            var memberProjectIds = _unitOfWork.ProjectMembers.GetAllQuery()
                .Where(pm => pm.UserId == currentUserId)
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

            var canAccess = await _membershipService.CanAccessProjectAsync(id, currentUserId, cancellationToken);
            if (!canAccess)
            {
                _logger.LogWarning("GetProjectById forbidden (Membership). UserId: {UserId}, ProjectId: {ProjectId}", currentUserId, id);
                throw new ForbiddenException("You are not a member of this project.");
            }

            return _mapper.Map<ProjectDetailsReadDto>(project);
        }

        public async Task<ProjectReadDto> CreateAsync(ProjectCreateDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            // FK validation before save (Validation step).
            var teamExists = await _unitOfWork.Teams.ExistsAsync(t => t.Id == dto.TeamId, cancellationToken);
            if (!teamExists)
                throw new NotFoundException("Team not found.");

            // Business Validation.
            if (dto.StartDate.HasValue && dto.EndDate.HasValue && dto.EndDate < dto.StartDate)
                throw new BadRequestException("End date cannot be before start date.");

            // Mapping.
            var project = _mapper.Map<Project>(dto);
            project.CreatedByUserId = currentUserId;

            // Repository.
            await _unitOfWork.Projects.AddAsync(project, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            // MEMBERSHIP: same pattern as TeamService.CreateAsync - creating the first Owner
            // is this method's job, not IMembershipService.AddProjectMemberAsync's.
            var ownerMembership = new ProjectMember
            {
                ProjectId = project.Id,
                UserId = currentUserId,
                Role = MembershipRole.Owner
            };
            await _unitOfWork.ProjectMembers.AddAsync(ownerMembership, cancellationToken);

            // Audit: Create is Save -> Audit -> Save, since project.Id only exists after the first save.
            var newValues = JsonSerializer.Serialize(new
            {
                project.Name,
                project.Description,
                project.TeamId,
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
            // Load Entity.
            var project = await _unitOfWork.Projects.GetByIdAsync(id, cancellationToken);
            if (project == null)
            {
                _logger.LogWarning("UpdateProject failed. Project not found.ProjectId:{ProjectId}", id);
                throw new NotFoundException("Project not found.");
            }

            // MEMBERSHIP: Permission (ProjectsUpdate, checked at the Controller) && Membership
            // (must be Owner/Manager of THIS project) - both have to succeed. This replaces the
            // old "Projects have no Ownership concept" note - that was true before the
            // Membership System existed; Membership is a distinct, additional check from
            // Identity Permissions, not the old ownership-by-CreatedByUserId pattern.
            await _membershipService.EnsureCanManageProjectAsync(id, currentUserId, cancellationToken);

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
            // Load Entity.
            var project = await _unitOfWork.Projects.GetByIdAsync(id, cancellationToken);
            if (project == null)
            {
                _logger.LogWarning("DeleteProject failed. Project not found. ProjectId: {ProjectId}", id);
                throw new NotFoundException("Project not found.");
            }

            // MEMBERSHIP: same as UpdateAsync above - Permission && Membership.
            await _membershipService.EnsureCanManageProjectAsync(id, currentUserId, cancellationToken);

            // Repository.
            _unitOfWork.Projects.Delete(project);

            // Audit: Delete is Audit -> Save, since the Id is already known.
            var oldValues = JsonSerializer.Serialize(new
            {
                project.Name,
                project.Description,
                project.TeamId,
                project.StartDate,
                project.EndDate,
                project.IsArchived
            });
            await _auditLogService.LogAsync(currentUserId, "Delete Project", nameof(Project), id.ToString(), oldValues, null);

            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Project deleted successfully. ProjectId: {ProjectId},CurrentUserId: {UserId}", id, currentUserId);
        }
    }
}
