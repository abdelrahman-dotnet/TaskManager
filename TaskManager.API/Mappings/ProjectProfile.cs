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
                .ForMember(d => d.TeamIds, o => o.MapFrom(s => s.ProjectTeams.Select(pt => pt.TeamId)))
                .ForMember(d => d.TeamNames, o => o.MapFrom(s => s.ProjectTeams.Select(pt => pt.Team.Name)));

            CreateMap<Project, ProjectDetailsReadDto>()
                .ForMember(d => d.TeamIds, o => o.MapFrom(s => s.ProjectTeams.Select(pt => pt.TeamId)))
                .ForMember(d => d.TeamNames, o => o.MapFrom(s => s.ProjectTeams.Select(pt => pt.Team.Name)))
                .ForMember(d => d.TasksCount, o => o.MapFrom(s => s.Tasks.Count))
                .ForMember(d => d.CompletedTasksCount, o => o.MapFrom(s => s.Tasks.Count(t => t.Status == TaskItemStatus.Done)));

            CreateMap<ProjectCreateDto, Project>();
            CreateMap<ProjectUpdateDto, Project>();
        }
    }
}