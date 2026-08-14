using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.Team;
using TaskManager.API.Helpers;

namespace TaskManager.Business.Services.Interfaces
{
    public interface ITeamService
    {
        // VISIBILITY (read-listing): results are filtered to teams the user is a member
        // of (IN subquery against TeamMembers). There is no cross-workspace bypass:
        // the business permission catalog has no Teams.ManageAny.
        Task<PagedResult<TeamReadDto>> GetAllAsync(TeamQueryParams queryParams, string currentUserId, CancellationToken cancellationToken = default);

        // PIPELINE (Auth Pipeline): Visibility -> Permission (WorkspaceView for reads).
        Task<TeamReadDto> GetByIdAsync(long id, string currentUserId, CancellationToken cancellationToken = default);

        // PIPELINE (Auth Pipeline): Visibility -> Permission (Teams.Create) -> Operation.
        // workspaceId comes from the route (POST /api/teams/{workspaceId}) - create DTOs
        // carry no workspace context.
        Task<TeamReadDto> CreateAsync(TeamCreateDto dto, long workspaceId, string currentUserId, CancellationToken cancellationToken = default);

        // PIPELINE (Auth Pipeline): Visibility -> Permission (Teams.Update) -> Operation.
        Task<TeamReadDto> UpdateAsync(long id, TeamUpdateDto dto, string currentUserId, CancellationToken cancellationToken = default);

        // PIPELINE (Auth Pipeline): Visibility -> Permission (Teams.Delete) -> Operation.
        Task DeleteAsync(long id, string currentUserId, CancellationToken cancellationToken = default);
    }
}
