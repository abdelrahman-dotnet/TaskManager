using AutoMapper;
using TaskManager.API.DTOs.AuditLog;
using TaskManager.Data.Entities;

namespace TaskManager.API.Mapping
{
    public class AuditLogProfile : Profile
    {
        public AuditLogProfile()
        {
            CreateMap<AuditLog, AuditLogReadDto>();
        }
    }
}
