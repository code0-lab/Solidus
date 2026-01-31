using AutoMapper;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using DomusMercatoris.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DomusMercatorisDotnetRest.Services
{
    public class CategoryService
    {
        private readonly DomusDbContext _db;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public CategoryService(DomusDbContext db, IMapper mapper, ICurrentUserService currentUserService)
        {
            _db = db;
            _mapper = mapper;
            _currentUserService = currentUserService;
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

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var category = await BaseQuery().FirstOrDefaultAsync(c => c.Id == id);
            return category == null ? null : _mapper.Map<CategoryDto>(category);
        }
    }
}
