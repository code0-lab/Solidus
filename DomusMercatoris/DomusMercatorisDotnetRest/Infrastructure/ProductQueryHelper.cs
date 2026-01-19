using AutoMapper;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DomusMercatorisDotnetRest.Infrastructure
{
    public static class ProductQueryHelper
    {
        public static IQueryable<Product> BaseProductQuery(DomusDbContext db)
        {
            return db.Products
                .Include(p => p.Categories)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .AsQueryable();
        }

        public static IQueryable<Product> ApplyCompanyFilter(IQueryable<Product> query, int? companyId)
        {
            if (companyId.HasValue) query = query.Where(p => p.CompanyId == companyId.Value);
            return query;
        }

        public static IQueryable<Product> ApplyBrandFilter(IQueryable<Product> query, int? brandId)
        {
            if (brandId.HasValue) query = query.Where(p => p.BrandId == brandId.Value);
            return query;
        }

        public static async Task<PaginatedResult<ProductDto>> PaginateAndMapAsync(
            IQueryable<Product> query,
            int pageNumber,
            int pageSize,
            IMapper mapper)
        {
            var totalCount = await query.CountAsync();
            var list = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var dtoList = mapper.Map<List<ProductDto>>(list);
            return new PaginatedResult<ProductDto>
            {
                Items = dtoList,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
    }
}
