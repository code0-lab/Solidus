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
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.BrandName, opt => opt.MapFrom(src => src.Brand != null ? src.Brand.Name : null));

            // Category Mapping
            CreateMap<Category, CategoryDto>();

            // AutoCategory Mapping
            CreateMap<AutoCategory, AutoCategoryDto>();

            // Brand Mapping
            CreateMap<Brand, BrandDto>();
            CreateMap<CreateBrandDto, Brand>();
            CreateMap<UpdateBrandDto, Brand>();

            // Cargo Tracking Mapping
            CreateMap<CargoTracking, CargoTrackingDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}".Trim() : null));
            CreateMap<CreateCargoTrackingDto, CargoTracking>();

            // Variant Product Mapping
            CreateMap<VariantProduct, VariantProductDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name))
                .ForMember(dest => dest.BrandName, opt => opt.MapFrom(src => src.Product.Brand != null ? src.Product.Brand.Name : null))
                .ForMember(dest => dest.CategoryNames, opt => opt.MapFrom(src => src.Product.Categories.Select(c => c.Name).ToList()))
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Product.Quantity))
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Product.Images));
            CreateMap<CreateVariantProductDto, VariantProduct>();

            CreateMap<Banner, BannerDto>();
            CreateMap<Banner, BannerSummaryDto>();
        }
    }
}
