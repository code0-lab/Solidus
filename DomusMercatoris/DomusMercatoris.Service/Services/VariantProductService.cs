using AutoMapper;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using Microsoft.EntityFrameworkCore;

using DomusMercatoris.Service.Interfaces;

namespace DomusMercatoris.Service.Services
{
    public class VariantProductService
    {
        private readonly DomusDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public VariantProductService(DomusDbContext dbContext, IMapper mapper, ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<List<VariantProductDto>> GetVariantsByProductIdAsync(long productId)
        {
            if (_currentUserService.CompanyId.HasValue)
            {
                var product = await _dbContext.Products.FindAsync(productId);
                if (product == null || product.CompanyId != _currentUserService.CompanyId.Value)
                {
                    return new List<VariantProductDto>();
                }
            }

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

            if (variant != null && _currentUserService.CompanyId.HasValue && variant.Product.CompanyId != _currentUserService.CompanyId.Value)
            {
                return null;
            }

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

            // Validate Product exists
            var product = await _dbContext.Products.FindAsync(createDto.ProductId);
            if (product == null)
            {
                throw new ArgumentException("Product not found.");
            }

            if (_currentUserService.CompanyId.HasValue && product.CompanyId != _currentUserService.CompanyId.Value)
            {
                throw new UnauthorizedAccessException("Cannot create variant for another company's product.");
            }

            var variant = _mapper.Map<VariantProduct>(createDto);
            await _dbContext.SaveChangesAsync();

            // Reload to get Product info for mapping
            await _dbContext.Entry(variant).Reference(v => v.Product).LoadAsync();

            return _mapper.Map<VariantProductDto>(variant);
        }

        public async Task<VariantProductDto> UpdateVariantAsync(UpdateVariantProductDto updateDto)
        {
            var variant = await _dbContext.VariantProducts.FindAsync(updateDto.Id);
            if (variant == null || variant.ProductId != updateDto.ProductId)
            {
                throw new ArgumentException("Variant not found or product mismatch.");
            }

            variant.Color = updateDto.Color;
            variant.Price = updateDto.Price;
            variant.IsCustomizable = updateDto.IsCustomizable;
            if (!string.IsNullOrEmpty(updateDto.CoverImage))
            {
                variant.CoverImage = updateDto.CoverImage;
            }

            _dbContext.VariantProducts.Update(variant);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<VariantProductDto>(variant);
        }

        public async Task DeleteVariantAsync(long id)
        {
            var variant = await _dbContext.VariantProducts
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (variant != null)
            {
                if (_currentUserService.CompanyId.HasValue && variant.Product.CompanyId != _currentUserService.CompanyId.Value)
                {
                    throw new UnauthorizedAccessException("Cannot delete variant for another company.");
                }

                _dbContext.VariantProducts.Remove(variant);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
