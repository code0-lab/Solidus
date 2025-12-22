using AutoMapper;
using DomusMercatoris.Service.DTOs;
using DomusMercatoris.Core.Entities;

namespace DomusMercatoris.Service.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Comment Mapping
            CreateMap<Comment, CommentDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : string.Empty))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}".Trim() : string.Empty));
            
            CreateMap<CreateCommentDto, Comment>();
            CreateMap<UpdateCommentDto, Comment>();

            // User Mapping
            CreateMap<User, UserDto>();
            CreateMap<UserRegisterDto, User>();
            
            // Product Mapping
            CreateMap<Product, ProductDto>();

            // Category Mapping
            CreateMap<Category, CategoryDto>();
        }
    }
}
