using AutoMapper;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DomusMercatorisDotnetRest.Services
{
    public class CategoryService
    {
        private readonly DomusDbContext _db;
        private readonly IMapper _mapper;

        public CategoryService(DomusDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<List<CategoryDto>> GetAllAsync()
        {
            var list = await _db.Categories.Include(c => c.Children).ToListAsync();
            return _mapper.Map<List<CategoryDto>>(list);
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var category = await _db.Categories.Include(c => c.Children).FirstOrDefaultAsync(c => c.Id == id);
            return category == null ? null : _mapper.Map<CategoryDto>(category);
        }
    }
}
