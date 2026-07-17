using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.Role;
using TaskManager.API.Helpers;

namespace TaskManager.Business.Services.Interfaces
{
    public interface IRoleService
    {
        Task<PagedResult<RoleReadDto>> GetAllAsync(RoleQueryParams queryParams, CancellationToken cancellationToken = default);
        Task<RoleReadDto> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<RoleReadDto> CreateAsync(RoleCreateAndUpdateDto dto, string currentUserId, CancellationToken cancellationToken = default);
        Task<RoleReadDto> UpdateAsync(string id, RoleCreateAndUpdateDto dto, string currentUserId, CancellationToken cancellationToken = default);
        Task DeleteAsync(string id, string currentUserId, CancellationToken cancellationToken = default);
    }
}