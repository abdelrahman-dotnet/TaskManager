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
                .ForMember(d => d.MembersCount, o => o.MapFrom(s => s.TeamMembers.Count))
                .ForMember(d => d.ProjectsCount, o => o.MapFrom(s => s.ProjectTeams.Count))
                .ForMember(d => d.ProjectNames, o => o.MapFrom(s => s.ProjectTeams.Select(pt => pt.Team.Name)));

            CreateMap<TeamCreateDto, Team>();
            CreateMap<TeamUpdateDto, Team>();
        }
    }
}