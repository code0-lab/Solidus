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

        public async Task<PaginatedResult<ProductDto>> GetAllAsync(int pageNumber, int pageSize, int? companyId)
        {
            var query = ApplyCompanyFilter(BaseProductQuery(), companyId);
            return await ProductQueryHelper.PaginateAndMapAsync(query, pageNumber, pageSize, _mapper);
        }

        public async Task<ProductDto?> GetByIdAsync(long id)
        {
            var product = await BaseProductQuery().FirstOrDefaultAsync(p => p.Id == id);
            return product is null ? null : _mapper.Map<ProductDto>(product);
        }

        public async Task<PaginatedResult<ProductDto>> GetByCategoryAsync(int categoryId, int pageNumber, int pageSize, int? companyId)
        {
            var query = ApplyCompanyFilter(BaseProductQuery().Where(p => p.Categories.Any(c => c.Id == categoryId)), companyId);
            return await ProductQueryHelper.PaginateAndMapAsync(query, pageNumber, pageSize, _mapper);
        }

        public async Task<PaginatedResult<ProductDto>> GetByClusterAsync(int clusterId, int pageNumber, int pageSize, int? companyId)
        {
            var query = ApplyCompanyFilter(
                BaseProductQuery().Where(p => _db.ProductClusterMembers.Any(m => m.ProductId == p.Id && m.ProductClusterId == clusterId)),
                companyId
            );
            return await ProductQueryHelper.PaginateAndMapAsync(query, pageNumber, pageSize, _mapper);
        }

        public async Task<PaginatedResult<ProductDto>> SearchAsync(string queryText, int pageNumber, int pageSize, int? companyId)
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
            query = query.Where(p =>
                ((p.Name ?? string.Empty).ToLower().Contains(q)) ||
                ((p.Sku ?? string.Empty).ToLower().Contains(q)) ||
                ((p.Description ?? string.Empty).ToLower().Contains(q))
            );

            query = query.OrderByDescending(p => p.Id);
            return await ProductQueryHelper.PaginateAndMapAsync(query, pageNumber, pageSize, _mapper);
        }
    }
}
