using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.User;
using TaskManager.API.Helpers;

namespace TaskManager.Business.Services.Interfaces
{
    public interface IUserService
    {
        Task<PagedResult<UserReadDto>> GetAllAsync(UserQueryParams queryParams, CancellationToken cancellationToken = default);
        Task<UserReadDto> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<UserReadDto> UpdateAsync(string id, UserUpdateDto dto, string currentUserId, bool canManageAny, CancellationToken cancellationToken = default);
        Task SetActiveStatusAsync(string id, UserStatusDto dto, string currentUserId, CancellationToken cancellationToken = default);
        Task DeleteAsync(string id, string currentUserId, CancellationToken cancellationToken = default);
        Task AssignRoleAsync(string userId, AssignRoleDto dto, string currentUserId, CancellationToken cancellationToken = default);
        Task RemoveRoleAsync(string userId, string roleName, string currentUserId, CancellationToken cancellationToken = default);
    }
}