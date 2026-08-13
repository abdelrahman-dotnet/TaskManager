using AutoMapper;
using TaskManager.API.DTOs.Comment;
using TaskManager.Data.Entities;

namespace TaskManager.API.Mapping
{
    public class CommentProfile : Profile
    {
        public CommentProfile()
        {
            CreateMap<Comment, CommentReadDto>()
                .ForMember(d => d.UserFullName, o => o.MapFrom(s => s.WorkspaceMember != null && s.WorkspaceMember.User != null ? s.WorkspaceMember.User.UserName : null));

            CreateMap<CommentCreateDto, Comment>();
            CreateMap<CommentUpdateDto, Comment>();
        }
    }
}
