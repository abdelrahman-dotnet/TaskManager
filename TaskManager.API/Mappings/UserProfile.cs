using AutoMapper;
using TaskManager.API.DTOs.User;
using TaskManager.Data.Entities;

namespace TaskManager.API.Mapping
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<ApplicationUser, UserReadDto>()
                .ForMember(d => d.TeamName, o => o.MapFrom(s => s.Team != null ? s.Team.Name : null))
                .ForMember(d => d.Roles, o => o.Ignore()); // filled in separately via UserManager.GetRolesAsync
        }
    }
}
