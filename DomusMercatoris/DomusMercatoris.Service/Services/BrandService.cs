using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using DomusMercatoris.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace DomusMercatoris.Service.Services
{
    public class BrandService
    {
        private readonly DomusDbContext _context;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public BrandService(DomusDbContext context, IMapper mapper, ICurrentUserService currentUserService)
        {
            _context = context;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<List<BrandDto>> GetBrandsByCompanyAsync(int companyId)
        {
            if (_currentUserService.CompanyId.HasValue && _currentUserService.CompanyId.Value != companyId)
            {
                return new List<BrandDto>();
            }

            var brands = await _context.Brands
                .Where(b => b.CompanyId == companyId)
                .OrderBy(b => b.Name)
                .ToListAsync();

            return _mapper.Map<List<BrandDto>>(brands);
        }

        public async Task<BrandDto?> GetBrandByIdAsync(int id, int companyId)
        {
            if (_currentUserService.CompanyId.HasValue && _currentUserService.CompanyId.Value != companyId)
            {
                return null;
            }

            var brand = await _context.Brands
                .FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == companyId);

            return brand == null ? null : _mapper.Map<BrandDto>(brand);
        }

        public async Task<BrandDto> CreateBrandAsync(CreateBrandDto dto)
        {
            if (_currentUserService.CompanyId.HasValue && dto.CompanyId != _currentUserService.CompanyId.Value)
            {
                throw new UnauthorizedAccessException("Cannot create brand for another company.");
            }

            var brand = _mapper.Map<Brand>(dto);
            
            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();

            return _mapper.Map<BrandDto>(brand);
        }

        public async Task<BrandDto?> UpdateBrandAsync(UpdateBrandDto dto, int companyId)
        {
            if (_currentUserService.CompanyId.HasValue && _currentUserService.CompanyId.Value != companyId)
            {
                return null;
            }

            var brand = await _context.Brands
                .FirstOrDefaultAsync(b => b.Id == dto.Id && b.CompanyId == companyId);

            if (brand == null) return null;

            brand.Name = dto.Name;
            brand.Description = dto.Description;
            // CompanyId should not change

            await _context.SaveChangesAsync();

            return _mapper.Map<BrandDto>(brand);
        }

        public async Task<bool> DeleteBrandAsync(int id, int companyId)
        {
            if (_currentUserService.CompanyId.HasValue && _currentUserService.CompanyId.Value != companyId)
            {
                return false;
            }

            var brand = await _context.Brands
                .Include(b => b.Products)
                .FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == companyId);

            if (brand == null) return false;

            if (brand.Products.Any())
            {
                // Optional: Prevent deletion if products exist, or just set BrandId to null (configured in DbContext)
                // DbContext OnDelete is SetNull, so we can delete safely.
            }

            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
