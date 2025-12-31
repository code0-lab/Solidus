using AutoMapper;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DomusMercatoris.Service.Services
{
    public class VariantProductService
    {
        private readonly DomusDbContext _dbContext;
        private readonly IMapper _mapper;

        public VariantProductService(DomusDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<List<VariantProductDto>> GetVariantsByProductIdAsync(long productId)
        {
            var variants = await _dbContext.VariantProducts
                .Include(v => v.Product)
                .Where(v => v.ProductId == productId)
                .ToListAsync();

            return _mapper.Map<List<VariantProductDto>>(variants);
        }

        public async Task<VariantProductDto?> GetVariantByIdAsync(long id)
        {
            var variant = await _dbContext.VariantProducts
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.Id == id);

            return _mapper.Map<VariantProductDto>(variant);
        }

        public async Task<VariantProductDto> CreateVariantAsync(CreateVariantProductDto createDto)
        {
            // Check max 5 variants
            var currentCount = await _dbContext.VariantProducts.CountAsync(v => v.ProductId == createDto.ProductId);
            if (currentCount >= 5)
            {
                throw new InvalidOperationException("A product can have at most 5 variants.");
            }

            var variant = _mapper.Map<VariantProduct>(createDto);
            
            // Validate Product exists
            var product = await _dbContext.Products.FindAsync(createDto.ProductId);
            if (product == null)
            {
                throw new ArgumentException("Product not found.");
            }

            _dbContext.VariantProducts.Add(variant);
            await _dbContext.SaveChangesAsync();

            // Reload to get Product info for mapping
            await _dbContext.Entry(variant).Reference(v => v.Product).LoadAsync();

            return _mapper.Map<VariantProductDto>(variant);
        }

        public async Task DeleteVariantAsync(long id)
        {
            var variant = await _dbContext.VariantProducts.FindAsync(id);
            if (variant != null)
            {
                _dbContext.VariantProducts.Remove(variant);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
