using System.Linq.Expressions;
using AutoMapper;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using DomusMercatoris.Service.Interfaces;
using DomusMercatorisDotnetRest.Infrastructure;
using Microsoft.EntityFrameworkCore;

using DomusMercatoris.Service.Services; // For BlacklistService

namespace DomusMercatorisDotnetRest.Services
{
    public class ProductService
    {
        private readonly DomusDbContext _db;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly BlacklistService _blacklistService;

        public ProductService(DomusDbContext db, IMapper mapper, ICurrentUserService currentUserService, BlacklistService blacklistService)
        {
            _db = db;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _blacklistService = blacklistService;
        }

        private int? GetEffectiveCompanyId(int? requestedCompanyId)
        {
            // If specific company is requested, use it
            if (requestedCompanyId.HasValue)
            {
                return requestedCompanyId.Value;
            }

            // If API Key user (CompanyApi role), STRICTLY enforce their company scope
            if (_currentUserService.IsInRole("CompanyApi") && _currentUserService.CompanyId.HasValue)
            {
                return _currentUserService.CompanyId.Value;
            }

            // For Human users (JWT), if they want All (null), give them All (null).
            // This allows logged-in users to see all products in the shop.
            return requestedCompanyId;
        }
        
        // Helper to apply blacklist filter
        // If User is logged in (Customer), they shouldn't see products from companies they blocked (Status 1 or 3)
        private async Task<IQueryable<Product>> ApplyBlacklistFilterAsync(IQueryable<Product> query)
        {
            if (_currentUserService.UserId.HasValue)
            {
                var blockedCompanyIds = await _blacklistService.GetCompaniesBlockedByCustomerAsync(_currentUserService.UserId.Value);
                if (blockedCompanyIds.Any())
                {
                    query = query.Where(p => !blockedCompanyIds.Contains(p.CompanyId));
                }
            }
            return query;
        }

        private IQueryable<Product> BaseProductQuery() => ProductQueryHelper.BaseProductQuery(_db);
        private static IQueryable<Product> ApplyCompanyFilter(IQueryable<Product> query, int? companyId) => ProductQueryHelper.ApplyCompanyFilter(query, companyId);
        private static IQueryable<Product> ApplyBrandFilter(IQueryable<Product> query, int? brandId) => ProductQueryHelper.ApplyBrandFilter(query, brandId);

        public async Task<PaginatedResult<ProductDto>> GetAllAsync(int pageNumber, int pageSize, int? companyId, int? brandId = null)
        {
            companyId = GetEffectiveCompanyId(companyId);
            var query = ApplyCompanyFilter(BaseProductQuery(), companyId);
            query = ApplyBrandFilter(query, brandId);
            query = await ApplyBlacklistFilterAsync(query);
            var result = await ProductQueryHelper.PaginateAndMapAsync(query, pageNumber, pageSize, _mapper);
            await SetBlacklistFlags(result);
            return result;
        }

        public async Task<ProductDto?> GetByIdAsync(long id)
        {
            var query = BaseProductQuery();
            
            // If restricted to a company, ensure we only return products from that company (or global ones if logic allows, but usually company-specific)
            // Note: BaseProductQuery might return all products. ApplyCompanyFilter logic handles null companyId (returns all) vs specific companyId.
            // But GetById logic usually just fetches by ID. If we want to secure it, we should apply filter too.
            var companyId = GetEffectiveCompanyId(null);
            if (companyId.HasValue)
            {
                query = ApplyCompanyFilter(query, companyId);
            }

            // Apply Blacklist Logic:
            // If user blocked company, they shouldn't see product (returns null or handled by frontend? User said "Product page reachable somehow -> show BLACK LIST button")
            // BUT: "Müşteri bu şirkete ait ürünleri görmeyecek" implies list view. 
            // "Bir şekilde müşteri ilgili ürünün sayfasına ulaşır ise sepete ekle butonunda BLACK LİST yazacak" implies GetById SHOULD return the product, 
            // but we need to know status. 
            // So GetById should return product, and frontend checks status via BlacklistService/Controller or a new field in ProductDto.
            // However, "Müşteri bu şirkete ait ürünleri görmeyecek" for Lists. 
            // So ApplyBlacklistFilterAsync is correct for GetAll, Search, etc.
            // For GetById, we return it, and let frontend handle UI.

            var product = await query.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return null;

            var dto = _mapper.Map<ProductDto>(product);

            if (_currentUserService.UserId.HasValue)
            {
                 // Check if Company blocked Customer
                 // We need to check if user is blocked by the company (CompanyBlockedCustomer)
                 if (!await _blacklistService.CanCustomerOrderAsync(_currentUserService.UserId.Value, product.CompanyId))
                 {
                     dto.IsBlockedByCompany = true;
                 }
            }

            return dto;
        }

        public async Task<PaginatedResult<ProductDto>> GetByCategoryAsync(int categoryId, int pageNumber, int pageSize, int? companyId, int? brandId = null)
        {
            companyId = GetEffectiveCompanyId(companyId);
            var query = ApplyCompanyFilter(BaseProductQuery().Where(p => p.Categories.Any(c => c.Id == categoryId)), companyId);
            query = ApplyBrandFilter(query, brandId);
            query = await ApplyBlacklistFilterAsync(query);
            var result = await ProductQueryHelper.PaginateAndMapAsync(query, pageNumber, pageSize, _mapper);
            await SetBlacklistFlags(result);
            return result;
        }

        public async Task<PaginatedResult<ProductDto>> GetByClusterAsync(int clusterId, int pageNumber, int pageSize, int? companyId, int? brandId = null, List<long>? prioritizedIds = null)
        {
            companyId = GetEffectiveCompanyId(companyId);
            var query = ApplyCompanyFilter(
                BaseProductQuery().Where(p => _db.ProductClusterMembers.Any(m => m.ProductId == p.Id && m.ProductClusterId == clusterId)),
                companyId
            );
            query = ApplyBrandFilter(query, brandId);
            query = await ApplyBlacklistFilterAsync(query);

            if (prioritizedIds != null && prioritizedIds.Any())
            {
                // Limit prioritized IDs to avoid expression tree depth issues
                var safeIds = prioritizedIds.Take(50).ToList();
                
                // Sort by similarity (index in the list)
                // p => p.Id == id[0] ? 0 : p.Id == id[1] ? 1 ... : int.MaxValue
                query = ApplySimilarityOrder(query, safeIds);
                
                // For non-prioritized items (rank = int.MaxValue), sort by ID descending
                query = ((IOrderedQueryable<Product>)query).ThenByDescending(p => p.Id);
            }
            else
            {
                query = query.OrderByDescending(p => p.Id);
            }

            return await ProductQueryHelper.PaginateAndMapAsync(query, pageNumber, pageSize, _mapper);
        }

        private IQueryable<Product> ApplySimilarityOrder(IQueryable<Product> query, List<long> ids)
        {
            if (ids == null || !ids.Any()) return query;

            var parameter = Expression.Parameter(typeof(Product), "p");
            var property = Expression.Property(parameter, "Id");
            
            Expression expr = Expression.Constant(int.MaxValue);
            
            // Build from last to first
            for (int i = ids.Count - 1; i >= 0; i--)
            {
                var idVal = Expression.Constant(ids[i]);
                var check = Expression.Equal(property, idVal);
                var rank = Expression.Constant(i);
                expr = Expression.Condition(check, rank, expr);
            }
            
            var lambda = Expression.Lambda<Func<Product, int>>(expr, parameter);
            
            return query.OrderBy(lambda);
        }

        public async Task<PaginatedResult<ProductDto>> SearchAsync(string queryText, int pageNumber, int pageSize, int? companyId, int? brandId = null)
        {
            companyId = GetEffectiveCompanyId(companyId);
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
            query = await ApplyBlacklistFilterAsync(query);
            
            // Prioritize name matches (Name contains query) -> then Description
            // Remove SKU from search
            
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

            var result = await ProductQueryHelper.PaginateAndMapAsync(query, pageNumber, pageSize, _mapper);
            await SetBlacklistFlags(result);
            return result;
        }

        private async Task SetBlacklistFlags(PaginatedResult<ProductDto> result)
        {
            if (!_currentUserService.UserId.HasValue) return;

            var blockingCompanyIds = await _blacklistService.GetCompaniesBlockingCustomerAsync(_currentUserService.UserId.Value);
            if (!blockingCompanyIds.Any()) return;

            foreach (var item in result.Items)
            {
                if (blockingCompanyIds.Contains(item.CompanyId))
                {
                    item.IsBlockedByCompany = true;
                }
            }
        }
    }
}
