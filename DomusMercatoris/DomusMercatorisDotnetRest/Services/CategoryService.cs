using AutoMapper;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using DomusMercatoris.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DomusMercatorisDotnetRest.Services
{
    public class CategoryService
    {
        private readonly DomusDbContext _db;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMemoryCache _cache;

        private const string AutoCategoriesCacheKey = "AutoCategories_Data";
        private const string AutoCategoriesTimestampKey = "AutoCategories_LastModified";

        public CategoryService(DomusDbContext db, IMapper mapper, ICurrentUserService currentUserService, IMemoryCache cache)
        {
            _db = db;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _cache = cache;
        }

        private IQueryable<Category> BaseQuery()
        {
            var query = _db.Categories.Include(c => c.Children).AsQueryable();
            
            if (_currentUserService.CompanyId.HasValue)
            {
                query = query.Where(c => c.CompanyId == _currentUserService.CompanyId.Value);
            }
            
            return query;
        }

        public async Task<List<CategoryDto>> GetAllAsync()
        {
            var list = await BaseQuery().ToListAsync();
            return _mapper.Map<List<CategoryDto>>(list);
        }

        public async Task<List<AutoCategoryDto>> GetAutoCategoriesAsync()
        {
            // Try to get from cache first - NO DB CALL unless cache is missing
            if (_cache.TryGetValue(AutoCategoriesCacheKey, out List<AutoCategoryDto>? cachedList))
            {
                return cachedList!;
            }

            // Fetch fresh data from DB
            var list = await _db.AutoCategories
                .AsNoTracking()
                .Include(ac => ac.Children)
                .Where(ac => ac.ParentId == null) // Get root auto categories
                .ToListAsync();

            var dtoList = _mapper.Map<List<AutoCategoryDto>>(list);

            // Set cache options
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetPriority(CacheItemPriority.High)
                .SetAbsoluteExpiration(TimeSpan.FromHours(1)); // 1 hour default expiration as fallback

            _cache.Set(AutoCategoriesCacheKey, dtoList, cacheOptions);

            return dtoList;
        }

        public void InvalidateAutoCategoriesCache()
        {
            _cache.Remove(AutoCategoriesCacheKey);
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var category = await BaseQuery().FirstOrDefaultAsync(c => c.Id == id);
            return category == null ? null : _mapper.Map<CategoryDto>(category);
        }
    }
}
