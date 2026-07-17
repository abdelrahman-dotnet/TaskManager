using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.Team;
using TaskManager.API.Helpers;

namespace TaskManager.Business.Services.Interfaces
{
    public interface ITeamService
    {
        Task<PagedResult<TeamReadDto>> GetAllAsync(TeamQueryParams queryParams, CancellationToken cancellationToken = default);
        Task<TeamReadDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<TeamReadDto> CreateAsync(TeamCreateDto dto, string managerId, CancellationToken cancellationToken = default);
        Task<TeamReadDto> UpdateAsync(long id, TeamUpdateDto dto, string currentUserId, CancellationToken cancellationToken = default);
        Task DeleteAsync(long id, string currentUserId, CancellationToken cancellationToken = default);
    }
}