using AutoMapper;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using DomusMercatorisDotnetRest.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DomusMercatorisDotnetRest.Services
{
    public class ProductService
    {
        private readonly DomusDbContext _db;
        private readonly IMapper _mapper;

        public ProductService(DomusDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        private IQueryable<Product> BaseProductQuery() => ProductQueryHelper.BaseProductQuery(_db);
        private static IQueryable<Product> ApplyCompanyFilter(IQueryable<Product> query, int? companyId) => ProductQueryHelper.ApplyCompanyFilter(query, companyId);
        private static IQueryable<Product> ApplyBrandFilter(IQueryable<Product> query, int? brandId) => ProductQueryHelper.ApplyBrandFilter(query, brandId);

        public async Task<PaginatedResult<ProductDto>> GetAllAsync(int pageNumber, int pageSize, int? companyId, int? brandId = null)
        {
            var query = ApplyCompanyFilter(BaseProductQuery(), companyId);
            query = ApplyBrandFilter(query, brandId);
            return await ProductQueryHelper.PaginateAndMapAsync(query, pageNumber, pageSize, _mapper);
        }

        public async Task<ProductDto?> GetByIdAsync(long id)
        {
            var product = await BaseProductQuery().FirstOrDefaultAsync(p => p.Id == id);
            return product is null ? null : _mapper.Map<ProductDto>(product);
        }

        public async Task<PaginatedResult<ProductDto>> GetByCategoryAsync(int categoryId, int pageNumber, int pageSize, int? companyId, int? brandId = null)
        {
            var query = ApplyCompanyFilter(BaseProductQuery().Where(p => p.Categories.Any(c => c.Id == categoryId)), companyId);
            query = ApplyBrandFilter(query, brandId);
            return await ProductQueryHelper.PaginateAndMapAsync(query, pageNumber, pageSize, _mapper);
        }

        public async Task<PaginatedResult<ProductDto>> GetByClusterAsync(int clusterId, int pageNumber, int pageSize, int? companyId, int? brandId = null, List<long>? prioritizedIds = null)
        {
            var query = ApplyCompanyFilter(
                BaseProductQuery().Where(p => _db.ProductClusterMembers.Any(m => m.ProductId == p.Id && m.ProductClusterId == clusterId)),
                companyId
            );
            query = ApplyBrandFilter(query, brandId);

            if (prioritizedIds != null && prioritizedIds.Any())
            {
                // Prioritize specified IDs (Top 3)
                var pIds = prioritizedIds.Take(3).ToList();
                
                // Start ordering
                // Note: We cast to IOrderedQueryable to chain ThenByDescending
                if (pIds.Count > 0)
                {
                    query = query.OrderByDescending(p => p.Id == pIds[0]);
                    
                    if (pIds.Count > 1)
                        query = ((IOrderedQueryable<Product>)query).ThenByDescending(p => p.Id == pIds[1]);
                    
                    if (pIds.Count > 2)
                        query = ((IOrderedQueryable<Product>)query).ThenByDescending(p => p.Id == pIds[2]);

                    query = ((IOrderedQueryable<Product>)query).ThenByDescending(p => p.Id);
                }
            }
            else
            {
                query = query.OrderByDescending(p => p.Id);
            }

            return await ProductQueryHelper.PaginateAndMapAsync(query, pageNumber, pageSize, _mapper);
        }

        public async Task<PaginatedResult<ProductDto>> SearchAsync(string queryText, int pageNumber, int pageSize, int? companyId, int? brandId = null)
        {
            var q = (queryText ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(q))
            {
                return new PaginatedResult<ProductDto>
                {
                    Items = new List<ProductDto>(),
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = 0
                };
            }

            q = q.ToLower();
            var query = ApplyCompanyFilter(BaseProductQuery(), companyId);
            query = ApplyBrandFilter(query, brandId);
            
            // Prioritize name matches (Name contains query) -> then Description
            // Remove SKU from search
            query = query.Where(p =>
                ((p.Name ?? string.Empty).ToLower().Contains(q)) ||
                ((p.Description ?? string.Empty).ToLower().Contains(q))
            );

            // Order by relevance: Name match first, then Id (newest/creation order usually)
            // Note: EF Core translates boolean comparison in OrderBy. 
            // True (1) > False (0) in descending order.
            query = query.OrderByDescending(p => (p.Name ?? string.Empty).ToLower().Contains(q))
                         .ThenByDescending(p => p.Id);

            return await ProductQueryHelper.PaginateAndMapAsync(query, pageNumber, pageSize, _mapper);
        }
    }
}
