using TaskManager.API.Exceptions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TaskManager.API.Config;
using TaskManager.API.Config.FiltersConfigs;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.User;
using TaskManager.API.Extentions;
using TaskManager.API.Helpers;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Business.UnitOfWork;
using TaskManager.Data.Entities;

namespace TaskManager.API.Services
{
    // Uses UserManager<ApplicationUser> directly instead of IUnitOfWork/IGenericRepository for
    // everything that touches Identity storage (create/update/delete/roles) - Identity already
    // owns that. IUnitOfWork is used only for the one thing Identity doesn't own: validating
    // TeamId against the Teams table, and staging the AuditLog row via IAuditLogService.
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogService _auditLogService;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;

        public UserService(
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork,
            IAuditLogService auditLogService,
            IMapper mapper,
            ILogger<UserService> logger)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _auditLogService = auditLogService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PagedResult<UserReadDto>> GetAllAsync(UserQueryParams queryParams, CancellationToken cancellationToken = default)
        {
            // UserManager.Users is an IQueryable<ApplicationUser> backed by the same DbContext,
            // so our existing ApplyFiltering/ApplySorting/ToPagedResultAsync extensions work on it unchanged.
            var query = _userManager.Users.AsNoTracking();

            query = query.ApplyFiltering(queryParams, UserFilterConfig.map);

            if (!string.IsNullOrWhiteSpace(queryParams.Search))
            {
                var search = queryParams.Search.Trim();
                query = query.Where(u =>
                    u.UserName.Contains(search) ||
                    u.Email != null && u.Email.Contains(search));
            }

            query = query.ApplySorting(queryParams.Sorts, AllowedSortingFields.Users, x => x.Id);

            var projected = query.ProjectTo<UserReadDto>(_mapper.ConfigurationProvider);
            var result = await projected.ToPagedResultAsync(queryParams.Page, queryParams.PageSize, cancellationToken);

            // Roles aren't stored on ApplicationUser itself (they live in AspNetUserRoles),
            // so AutoMapper can't project them directly - fill them in per row after paging.
            // NOTE (gap, not fixed here): UserManager.FindByIdAsync/GetRolesAsync don't accept a
            // CancellationToken - that's a limitation of Identity's UserManager API itself, not
            // something this Service can pass through. Flagging per your "no unresolved item
            // left unmentioned" instruction rather than silently leaving it half-done.
            foreach (var userDto in result.Data)
            {
                var user = await _userManager.FindByIdAsync(userDto.Id);
                if (user != null)
                    userDto.Roles = (await _userManager.GetRolesAsync(user)).ToList();
            }

            _logger.LogInformation("Users retrieved successfully. Count: {Count}", result.Data.Count);
            return result;
        }

        public async Task<UserReadDto> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            // Read-only - AsNoTracking added (this was missing before).
            var user = await _userManager.Users
                .AsNoTracking()
                .Include(u => u.Team)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("GetUserById failed. User not found. UserId: {UserId}", id);
                throw new NotFoundException("User not found.");
            }

            var dto = _mapper.Map<UserReadDto>(user);
            dto.Roles = (await _userManager.GetRolesAsync(user)).ToList();
            return dto;
        }

        public async Task<UserReadDto> UpdateAsync(string id, UserUpdateDto dto, string currentUserId, bool canManageAny, CancellationToken cancellationToken = default)
        {
            // Ownership check (Users is listed under the Ownership/ManageAny pattern in the
            // standard) - kept as the very first step per the CRUD order (Validation first).
            if (!canManageAny && id != currentUserId)
            {
                _logger.LogWarning("UpdateUser forbidden. UserId: {UserId} tried to edit UserId: {TargetUserId}", currentUserId, id);
                throw new ForbiddenException("You can only edit your own profile.");
            }

            // Load Entity
            var user = await _userManager.FindByIdAsync(id);

            // NotFound Check
            if (user == null)
            {
                _logger.LogWarning("UpdateUser failed. User not found. UserId: {UserId}", id);
                throw new NotFoundException("User not found.");
            }

            // Business Validation - FK check: TeamId must exist before saving, don't rely on a SQL exception.
            if (dto.TeamId != null && dto.TeamId != user.TeamId)
            {
                var teamExists = await _unitOfWork.Teams.ExistsAsync(t => t.Id == dto.TeamId, cancellationToken);
                if (!teamExists)
                    throw new NotFoundException("Team not found.");
            }

            var oldValues = JsonSerializer.Serialize(new
            {
                user.UserName,
                user.TeamId,
                user.ShouldNotify,
                user.NotifyPeriod
            });

            // Mapping
            _mapper.Map(dto, user);

            var newValues = JsonSerializer.Serialize(new
            {
                user.UserName,
                user.TeamId,
                user.ShouldNotify,
                user.NotifyPeriod
            });

            // Audit - staged before the Identity save call, since the Id is already known
            // (matches "Update/Delete: Audit then Save" from the standard).
            await _auditLogService.LogAsync(currentUserId, "Update", "User", id, oldValues, newValues);

            // Repository / persistence - UserManager.UpdateAsync is Identity's equivalent of
            // CompleteAsync here. NOTE (open item, not resolved here): this only actually
            // persists the AuditLog row added above in the same transaction if IUnitOfWork and
            // UserManager share the same underlying DbContext instance in this request's DI
            // scope. I can't confirm that without seeing the DI registration - please verify.
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new BadRequestException(result.Errors.Select(e => e.Description));

            // Logging
            _logger.LogInformation("User updated successfully. UserId: {UserId}, CurrentUserId: {CurrentUserId}", id, currentUserId);

            // Return DTO
            var readDto = _mapper.Map<UserReadDto>(user);
            readDto.Roles = (await _userManager.GetRolesAsync(user)).ToList();
            return readDto;
        }

        public async Task SetActiveStatusAsync(string id, UserStatusDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("SetActiveStatus failed. User not found. UserId: {UserId}", id);
                throw new NotFoundException("User not found.");
            }

            var oldIsActive = user.IsActive;
            user.IsActive = dto.IsActive;

            await _auditLogService.LogAsync(currentUserId, "SetActiveStatus", "User", id,
                oldValues: oldIsActive.ToString(),
                newValues: dto.IsActive.ToString());

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new BadRequestException(result.Errors.Select(e => e.Description));

            _logger.LogInformation("User active status changed. UserId: {UserId}, IsActive: {IsActive}, CurrentUserId: {CurrentUserId}", id, dto.IsActive, currentUserId);
        }

        public async Task DeleteAsync(string id, string currentUserId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("DeleteUser failed. User not found. UserId: {UserId}", id);
                throw new NotFoundException("User not found.");
            }

            var oldValues = JsonSerializer.Serialize(new { user.UserName, user.Email });

            await _auditLogService.LogAsync(currentUserId, "Delete", "User", id, oldValues: oldValues);

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                throw new BadRequestException(result.Errors.Select(e => e.Description));

            _logger.LogInformation("User deleted successfully. UserId: {UserId}, CurrentUserId: {CurrentUserId}", id, currentUserId);
        }

        public async Task AssignRoleAsync(string userId, AssignRoleDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("User not found.");

            if (await _userManager.IsInRoleAsync(user, dto.RoleName))
                throw new ConflictException("User already has this role.");

            await _auditLogService.LogAsync(currentUserId, "AssignRole", "User", userId, newValues: dto.RoleName);

            var result = await _userManager.AddToRoleAsync(user, dto.RoleName);
            if (!result.Succeeded)
                throw new BadRequestException(result.Errors.Select(e => e.Description));

            _logger.LogInformation("Role assigned. UserId: {UserId}, Role: {Role}, CurrentUserId: {CurrentUserId}", userId, dto.RoleName, currentUserId);
        }

        public async Task RemoveRoleAsync(string userId, string roleName, string currentUserId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("User not found.");

            if (!await _userManager.IsInRoleAsync(user, roleName))
                throw new NotFoundException("User does not have this role.");

            await _auditLogService.LogAsync(currentUserId, "RemoveRole", "User", userId, oldValues: roleName);

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (!result.Succeeded)
                throw new BadRequestException(result.Errors.Select(e => e.Description));

            _logger.LogInformation("Role removed. UserId: {UserId}, Role: {Role}, CurrentUserId: {CurrentUserId}", userId, roleName, currentUserId);
        }
    }
}