using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.Team;
using TaskManager.API.Helpers;

namespace TaskManager.Business.Services.Interfaces
{
    public interface ITeamService
    {
        // MEMBERSHIP: currentUserId added - results are filtered to teams the user is a
        // member of.
        // TODO: no bypass flag here, same reasoning as IProjectService.GetAllAsync - add
        // Teams.ManageAny (Permissions.cs + Seeder) separately if Admin bypass is wanted later.
        Task<PagedResult<TeamReadDto>> GetAllAsync(TeamQueryParams queryParams, string currentUserId, CancellationToken cancellationToken = default);
        Task<TeamReadDto> GetByIdAsync(long id, string currentUserId, CancellationToken cancellationToken = default);
        Task<TeamReadDto> CreateAsync(TeamCreateDto dto, string managerId, CancellationToken cancellationToken = default);
        Task<TeamReadDto> UpdateAsync(long id, TeamUpdateDto dto, string currentUserId, CancellationToken cancellationToken = default);
        Task DeleteAsync(long id, string currentUserId, CancellationToken cancellationToken = default);
    }
}
