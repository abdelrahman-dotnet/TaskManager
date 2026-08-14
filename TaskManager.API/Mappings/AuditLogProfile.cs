using AutoMapper;
using TaskManager.API.DTOs.AuditLog;
using TaskManager.Data.Entities;

namespace TaskManager.API.Mapping
{
    public class AuditLogProfile : Profile
    {
        public AuditLogProfile()
        {
            CreateMap<AuditLog, AuditLogReadDto>()
                .ForMember(d => d.WorkspaceMemberId, o => o.MapFrom(s => s.WorkspaceMemberId))
                .ForMember(d => d.ActorUserName, o => o.MapFrom(s => s.WorkspaceMember != null ? s.WorkspaceMember.User!.UserName : null));
        }
    }
}
