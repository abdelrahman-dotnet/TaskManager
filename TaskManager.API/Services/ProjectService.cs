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

        public ProjectService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ProjectService> logger, IAuditLogService auditLogService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _auditLogService = auditLogService;
        }

        public async Task<PagedResult<ProjectReadDto>> GetAllAsync(ProjectQueryParams queryParams, CancellationToken cancellationToken = default)
        {
            var query = _unitOfWork.Projects.GetAllQuery().AsNoTracking();

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

        public async Task<ProjectDetailsReadDto> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            var project = await _unitOfWork.Projects.GetDetailsAsync(id, cancellationToken);
            if (project == null)
            {
                _logger.LogWarning("GetProjectById failed. Project not found. ProjectId: {ProjectId}", id);
                throw new NotFoundException("Project not found.");
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

            // Audit: Create is Save -> Audit -> Save, since project.Id only exists after the first save.
            var newValues = JsonSerializer.Serialize(new
            {
                project.Name,
                project.Description,
                project.TeamId,
                project.StartDate,
                project.EndDate
            });
            await _auditLogService.LogAsync(currentUserId,"Create Project", nameof(Project), project.Id.ToString(), null, newValues);
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
                _logger.LogWarning("UpdateProject failed. Project not found.ProjectId:{ProjectId}",id);
                throw new NotFoundException("Project not found.");
            }

            // No Ownership/Permission check here by design - Projects aren't a Personal Resource
            // (see IProjectService.cs). The endpoint itself is gated by
            // [Authorize(Policy = Permissions.ProjectsUpdate)] at the Controller.

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

            // No Ownership/Permission check here by design - see UpdateAsync above.

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