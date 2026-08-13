using AutoMapper;
using TaskManager.API.DTOs.Attachment;
using TaskManager.Data.Entities;

namespace TaskManager.API.Mapping
{
    public class AttachmentProfile : Profile
    {
        public AttachmentProfile()
        {
            CreateMap<Attachment, AttachmentReadDto>()
                .ForMember(d => d.UploadedByUserName, o => o.MapFrom(s => s.UploadedByWorkspaceMember != null && s.UploadedByWorkspaceMember.User != null ? s.UploadedByWorkspaceMember.User.UserName : null));

            CreateMap<AttachmentCreateDto, Attachment>();
        }
    }
}
