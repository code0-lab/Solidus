using AutoMapper;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DomusMercatorisDotnetRest.Services
{
    public class CompanyService
    {
        private readonly DomusDbContext _db;
        private readonly IMapper _mapper;

        public CompanyService(DomusDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<List<CompanyDto>> GetAllAsync()
        {
            if (!await _db.Companies.AnyAsync())
            {
                var defaultCompany = new Company
                {
                    Name = "Domus Mercatoris",
                    CreatedAt = DateTime.UtcNow
                };
                _db.Companies.Add(defaultCompany);
                await _db.SaveChangesAsync();
            }

            var companies = await _db.Companies.OrderBy(c => c.Name).ToListAsync();
            return _mapper.Map<List<CompanyDto>>(companies);
        }
    }
}
