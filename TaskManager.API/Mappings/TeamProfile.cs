using AutoMapper;
using TaskManager.API.DTOs.Team;
using TaskManager.Data.Entities;

namespace TaskManager.API.Mapping
{
    public class TeamProfile : Profile
    {
        public TeamProfile()
        {
            CreateMap<Team, TeamReadDto>()
                .ForMember(d => d.ManagerName, o => o.MapFrom(s => s.Manager != null ? s.Manager.UserName : null))
                .ForMember(d => d.MembersCount, o => o.MapFrom(s => s.Members.Count))
                .ForMember(d => d.ProjectsCount, o => o.MapFrom(s => s.Projects.Count));

            CreateMap<TeamCreateDto, Team>();
            CreateMap<TeamUpdateDto, Team>();
        }
    }
}