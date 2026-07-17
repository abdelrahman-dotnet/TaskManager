using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.Project;
using TaskManager.API.Helpers;

namespace TaskManager.Business.Services.Interfaces
{
    public interface IProjectService
    {
        Task<PagedResult<ProjectReadDto>> GetAllAsync(ProjectQueryParams queryParams, CancellationToken cancellationToken = default);
        Task<ProjectDetailsReadDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<ProjectReadDto> CreateAsync(ProjectCreateDto dto, string currentUserId, CancellationToken cancellationToken = default);
        Task<ProjectReadDto> UpdateAsync(long id, ProjectUpdateDto dto, string currentUserId, CancellationToken cancellationToken = default);
        Task DeleteAsync(long id, string currentUserId, CancellationToken cancellationToken = default);
    }
}