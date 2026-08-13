using AutoMapper;
using TaskManager.API.DTOs.TeamMember;
using TaskManager.Data.Entities;

namespace TaskManager.API.Mapping
{
    public class TeamMemberProfile : Profile
    {
        public TeamMemberProfile()
        {
            CreateMap<TeamMember, TeamMemberReadDto>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.WorkspaceMember != null && s.WorkspaceMember.User != null ? s.WorkspaceMember.User.UserName : null));

            // Convenience map for the Service - TeamId/Id/CreatedAt are still set explicitly
            // there (TeamId comes from the route, not the DTO).
            CreateMap<AddTeamMemberDto, TeamMember>();
        }
    }
}
