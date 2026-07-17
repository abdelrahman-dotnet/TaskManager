using AutoMapper;
using TaskManager.API.DTOs.Project;
using TaskManager.Data.Entities;
namespace TaskManager.API.Mapping
{
    public class ProjectProfile : Profile
    {
        public ProjectProfile()
        {
            CreateMap<Project, ProjectReadDto>()
                .ForMember(d => d.TeamName, o => o.MapFrom(s => s.Team != null ? s.Team.Name : null));

            CreateMap<Project, ProjectDetailsReadDto>()
                .ForMember(d => d.TeamName, o => o.MapFrom(s => s.Team != null ? s.Team.Name : null))
                .ForMember(d => d.TasksCount, o => o.MapFrom(s => s.Tasks.Count))
                .ForMember(d => d.CompletedTasksCount, o => o.MapFrom(s => s.Tasks.Count(t => t.Status == TaskItemStatus.Done)));

            CreateMap<ProjectCreateDto, Project>();
            CreateMap<ProjectUpdateDto, Project>();
        }
    }
}
