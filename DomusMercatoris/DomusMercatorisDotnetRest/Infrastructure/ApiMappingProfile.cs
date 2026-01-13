using AutoMapper;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Service.DTOs;

namespace DomusMercatorisDotnetRest.Infrastructure
{
    public class ApiMappingProfile : Profile
    {
        public ApiMappingProfile()
        {
            CreateMap<Company, CompanyDto>();
        }
    }
}
