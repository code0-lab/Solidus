using AutoMapper;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using DomusMercatoris.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DomusMercatorisDotnetRest.Services
{
    public class CompanyService
    {
        private readonly DomusDbContext _db;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public CompanyService(DomusDbContext db, IMapper mapper, ICurrentUserService currentUserService)
        {
            _db = db;
            _mapper = mapper;
            _currentUserService = currentUserService;
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

            var query = _db.Companies.AsQueryable();

            if (_currentUserService.CompanyId.HasValue)
            {
                query = query.Where(c => c.CompanyId == _currentUserService.CompanyId.Value);
            }

            var companies = await query.OrderBy(c => c.Name).ToListAsync();
            return _mapper.Map<List<CompanyDto>>(companies);
        }
    }
}
