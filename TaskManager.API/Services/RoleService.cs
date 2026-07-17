using TaskManager.API.Exceptions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TaskManager.API.Config;
using TaskManager.API.Config.FiltersConfigs;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.Role;
using TaskManager.API.Extentions;
using TaskManager.API.Helpers;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Business.UnitOfWork;
using TaskManager.Data.Entities;

namespace TaskManager.API.Services
{
    // Uses RoleManager<ApplicationRole> instead of IUnitOfWork/IGenericRepository, same reasoning as UserService.
    // FIX: IUnitOfWork is injected here ONLY to call CompleteAsync() after AuditLogService.LogAsync() -
    // RoleManager.CreateAsync/UpdateAsync/DeleteAsync already persist the Role itself internally
    // (Identity does its own SaveChanges), but AuditLogService.LogAsync only tracks the new AuditLog
    // row without saving (same as it's used in ProjectService) - something has to call SaveChanges
    // for that row to actually reach the database, and RoleService had no such hook before.
    public class RoleService : IRoleService
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<RoleService> _logger;
        private readonly IAuditLogService _auditLogService;

        public RoleService(
            RoleManager<ApplicationRole> roleManager,
            IUnitOfWork unitOfWork,
            IAuditLogService auditLogService,
            IMapper mapper,
            ILogger<RoleService> logger)
        {
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _auditLogService = auditLogService;
        }

        public async Task<PagedResult<RoleReadDto>> GetAllAsync(RoleQueryParams queryParams, CancellationToken cancellationToken = default)
        {
            var query = _roleManager.Roles.AsNoTracking();

            query = query.ApplyFiltering(queryParams, RoleFilterConfig.map);

            if (!string.IsNullOrWhiteSpace(queryParams.Search))
            {
                var search = queryParams.Search;
                search = search.Trim();
                query = query.Where(r => r.Name != null && r.Name.Contains(search));
            }

            // FIX: was missing the default-sort fallback every other GetAllAsync now has -
            // without it, pagination has no guaranteed row order when no Sort is requested.
            query = query.ApplySorting(queryParams.Sorts, AllowedSortingFields.Roles, x => x.Id);

            var projected = query.ProjectTo<RoleReadDto>(_mapper.ConfigurationProvider);
            var result = await projected.ToPagedResultAsync(queryParams.Page, queryParams.PageSize, cancellationToken);

            _logger.LogInformation("Roles retrieved successfully. Count: {Count}", result.Data.Count);
            return result;
        }

        public async Task<RoleReadDto> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            // NOTE: RoleManager's methods (FindByIdAsync, RoleExistsAsync, CreateAsync, UpdateAsync,
            // DeleteAsync) don't expose CancellationToken overloads in ASP.NET Core Identity - this
            // is a limitation of Identity itself, not something skipped here. cancellationToken is
            // still accepted on every method for interface consistency and is used everywhere it
            // actually can be (the LINQ query in GetAllAsync above, and CompleteAsync below).
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                _logger.LogWarning("GetRoleById failed. Role not found. RoleId: {RoleId}", id);
                throw new NotFoundException("Role not found.");
            }

            return _mapper.Map<RoleReadDto>(role);
        }

        public async Task<RoleReadDto> CreateAsync(RoleCreateAndUpdateDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            var exists = await _roleManager.RoleExistsAsync(dto.Name);
            dto.Name = dto.Name.Trim();
            if (exists)
                throw new ConflictException("A role with this name already exists.");

            var role = _mapper.Map<ApplicationRole>(dto);

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
                throw new BadRequestException(string.Join("; ", result.Errors.Select(e => e.Description)));

           
            var newValues = JsonSerializer.Serialize(new { role.Name, role.Description });
            await _auditLogService.LogAsync(currentUserId, "Create Role", nameof(ApplicationRole), role.Id, null, newValues);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Role created successfully. RoleId: {RoleId}, Name: {Name}, UserId: {UserId}", role.Id, role.Name, currentUserId);
            return _mapper.Map<RoleReadDto>(role);
        }

        public async Task<RoleReadDto> UpdateAsync(string id, RoleCreateAndUpdateDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                _logger.LogWarning("UpdateRole failed. Role not found. RoleId: {RoleId}", id);
                throw new NotFoundException("Role not found.");
            }

            var oldValues = JsonSerializer.Serialize(new { role.Name, role.Description });

            if (!string.Equals(role.Name, dto.Name, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await _roleManager.RoleExistsAsync(dto.Name);

                if (exists)
                    throw new ConflictException("A role with this name already exists.");
            }
            _mapper.Map(dto, role);

            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
                throw new BadRequestException(string.Join("; ", result.Errors.Select(e => e.Description)));

            // Audit: Update is Audit -> Save, since the Id is already known.
            var newValues = JsonSerializer.Serialize(new { role.Name, role.Description });
            await _auditLogService.LogAsync(currentUserId, "Update Role", "Role", id, oldValues, newValues);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Role updated successfully. RoleId: {RoleId}, UserId: {UserId}", id, currentUserId);
            return _mapper.Map<RoleReadDto>(role);
        }

        public async Task DeleteAsync(string id, string currentUserId, CancellationToken cancellationToken = default)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                _logger.LogWarning("DeleteRole failed. Role not found. RoleId: {RoleId}", id);
                throw new NotFoundException("Role not found.");
            }

            var oldValues = JsonSerializer.Serialize(new { role.Name, role.Description });

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
                throw new BadRequestException(string.Join("; ", result.Errors.Select(e => e.Description)));

            // Audit: Delete is Audit -> Save, since the Id is already known.
            await _auditLogService.LogAsync(currentUserId, "Delete Role", "Role", id, oldValues, null);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Role deleted successfully. RoleId: {RoleId}, UserId: {UserId}", id, currentUserId);
        }
    }
}