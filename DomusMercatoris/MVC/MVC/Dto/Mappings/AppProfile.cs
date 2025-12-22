using AutoMapper;
using DomusMercatorisDotnetMVC.Models;
using DomusMercatorisDotnetMVC.Dto.ProductDto;
using DomusMercatorisDotnetMVC.Dto.UserDto;
using CommentDtos = DomusMercatorisDotnetMVC.Dto.CommentsDto;

namespace DomusMercatorisDotnetMVC.Dto.Mappings
{
    public class AppProfile : Profile
    {
        public AppProfile()
        {
            CreateMap<UserRegisterDto, User>();
            CreateMap<ProductCreateDto, Product>();
            
            CreateMap<CommentModel, CommentDtos.CommentsDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : "Unknown"));
            
            CreateMap<CommentDtos.CreateCommentDto, CommentModel>();
        }
    }
}
