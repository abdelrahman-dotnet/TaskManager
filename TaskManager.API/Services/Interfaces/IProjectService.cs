using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.Project;
using TaskManager.API.Helpers;

namespace TaskManager.Business.Services.Interfaces
{
    public interface IProjectService
    {
        // MEMBERSHIP: currentUserId added - results are filtered to projects the user is a
        // member of.
        // TODO: no bypass flag here (unlike Task's canManageAny) because Permissions.cs has no
        // Projects.ManageAny yet. If Admins/Managers should see every project regardless of
        // membership, add Projects.ManageAny (Permissions.cs + Seeder) as a separate decision,
        // then thread it through here the same way canManageAny works for Tasks.
        Task<PagedResult<ProjectReadDto>> GetAllAsync(ProjectQueryParams queryParams, string currentUserId, CancellationToken cancellationToken = default);
        Task<ProjectDetailsReadDto> GetByIdAsync(long id, string currentUserId, CancellationToken cancellationToken = default);
        Task<ProjectReadDto> CreateAsync(ProjectCreateDto dto, string currentUserId, CancellationToken cancellationToken = default);
        Task<ProjectReadDto> UpdateAsync(long id, ProjectUpdateDto dto, string currentUserId, CancellationToken cancellationToken = default);
        Task DeleteAsync(long id, string currentUserId, CancellationToken cancellationToken = default);
    }
}
