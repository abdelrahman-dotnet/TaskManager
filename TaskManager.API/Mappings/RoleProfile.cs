using AutoMapper;
using TaskManager.API.DTOs.Role;
using TaskManager.Data.Entities;

namespace TaskManager.API.Mapping
{
    public class RoleProfile : Profile
    {
        public RoleProfile()
        {
            CreateMap<ApplicationRole, RoleReadDto>();
        }
    }
}
