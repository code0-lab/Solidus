using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace DomusMercatoris.Service.Services
{
    public class BrandService
    {
        private readonly DomusDbContext _context;
        private readonly IMapper _mapper;

        public BrandService(DomusDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<BrandDto>> GetBrandsByCompanyAsync(int companyId)
        {
            var brands = await _context.Brands
                .Where(b => b.CompanyId == companyId)
                .OrderBy(b => b.Name)
                .ToListAsync();

            return _mapper.Map<List<BrandDto>>(brands);
        }

        public async Task<BrandDto?> GetBrandByIdAsync(int id, int companyId)
        {
            var brand = await _context.Brands
                .FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == companyId);

            return brand == null ? null : _mapper.Map<BrandDto>(brand);
        }

        public async Task<BrandDto> CreateBrandAsync(CreateBrandDto dto)
        {
            var brand = _mapper.Map<Brand>(dto);
            
            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();

            return _mapper.Map<BrandDto>(brand);
        }

        public async Task<BrandDto?> UpdateBrandAsync(UpdateBrandDto dto, int companyId)
        {
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
