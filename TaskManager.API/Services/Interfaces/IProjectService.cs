using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.Project;
using TaskManager.API.Helpers;

namespace TaskManager.Business.Services.Interfaces
{
    public interface IProjectService
    {
        // VISIBILITY (read-listing): results are filtered to projects the user is a member
        // of (IN subquery against ProjectMembers). There is no cross-workspace bypass:
        // the business permission catalog has no Projects.ManageAny.
        Task<PagedResult<ProjectReadDto>> GetAllAsync(ProjectQueryParams queryParams, string currentUserId, CancellationToken cancellationToken = default);

        // PIPELINE (Auth Pipeline): Visibility -> Permission (WorkspaceView for reads).
        Task<ProjectDetailsReadDto> GetByIdAsync(long id, string currentUserId, CancellationToken cancellationToken = default);

        // PIPELINE (Auth Pipeline): Visibility -> Permission (Projects.Create) -> Operation.
        // workspaceId comes from the route (POST /api/projects/{workspaceId}) - create DTOs
        // carry no workspace context.
        Task<ProjectReadDto> CreateAsync(ProjectCreateDto dto, long workspaceId, string currentUserId, CancellationToken cancellationToken = default);

        // PIPELINE (Auth Pipeline): Visibility -> Permission (Projects.Update) -> Operation.
        Task<ProjectReadDto> UpdateAsync(long id, ProjectUpdateDto dto, string currentUserId, CancellationToken cancellationToken = default);

        // PIPELINE (Auth Pipeline): Visibility -> Permission (Projects.Delete) -> Condition (ProjectArchived) -> Operation.
        Task DeleteAsync(long id, string currentUserId, CancellationToken cancellationToken = default);

        // PIPELINE (Auth Pipeline): Visibility -> Permission (Projects.Archive) -> Operation.
        // Restoring an archived project is a regular Update (Projects.Update) - no separate permission.
        Task<ProjectReadDto> ArchiveAsync(long id, string currentUserId, CancellationToken cancellationToken = default);
        Task<ProjectReadDto> RestoreAsync(long id, string currentUserId, CancellationToken cancellationToken = default);

        // PIPELINE (Auth Pipeline): Visibility -> Permission (Projects.ManageTeams) -> BR (same workspace).
        Task AttachTeamAsync(long projectId, long teamId, string currentUserId, CancellationToken cancellationToken = default);
        Task DetachTeamAsync(long projectId, long teamId, string currentUserId, CancellationToken cancellationToken = default);
    }
}
