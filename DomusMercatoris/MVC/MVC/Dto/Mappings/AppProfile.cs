using AutoMapper;
using DomusMercatoris.Core.Entities;
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
            
            CreateMap<Comment, CommentDtos.CommentsDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : "Unknown"))
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : string.Empty))
                .ForMember(dest => dest.Comment, opt => opt.MapFrom(src => src.Text));
            
            CreateMap<CommentDtos.CreateCommentDto, Comment>()
                .ForMember(dest => dest.Text, opt => opt.MapFrom(src => src.Comment));
        }
    }
}
