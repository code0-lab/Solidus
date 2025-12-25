using Microsoft.AspNetCore.Mvc;
using DomusMercatoris.Data;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Service.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DomusMercatorisDotnetRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly DomusDbContext _db;

        public ProductsController(DomusDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<PaginatedResult<ProductDto>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 9)
        {
            var query = _db.Products.Include(p => p.Categories).AsQueryable();

            var totalCount = await query.CountAsync();
            var list = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            var dtoList = list.Select(MapToDto).ToList();

            return Ok(new PaginatedResult<ProductDto>
            {
                Items = dtoList,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<ProductDto>> GetById(long id)
        {
            var product = await _db.Products
                .Include(p => p.Categories)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (product is null) return NotFound();
            
            return Ok(MapToDto(product));
        }

        [HttpGet("by-category/{categoryId:int}")]
        public async Task<ActionResult<PaginatedResult<ProductDto>>> GetByCategory(int categoryId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 9)
        {
            var query = _db.Products
                .Include(p => p.Categories)
                .Where(p => p.Categories.Any(c => c.Id == categoryId))
                .AsQueryable();

            var totalCount = await query.CountAsync();
            var list = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            var dtoList = list.Select(MapToDto).ToList();

            return Ok(new PaginatedResult<ProductDto>
            {
                Items = dtoList,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }

        private static ProductDto MapToDto(Product product)
        {
            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Sku = product.Sku,
                Description = product.Description,
                Price = product.Price,
                Quantity = product.Quantity,
                Images = product.Images,
                Categories = product.Categories.Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    ParentId = c.ParentId
                    // Children mapping is skipped here to avoid potential circular depth issues in simple product view
                }).ToList()
            };
        }
    }
}
