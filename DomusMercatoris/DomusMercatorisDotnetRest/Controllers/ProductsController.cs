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
        public async Task<ActionResult<PaginatedResult<ProductDto>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 9, [FromQuery] int? companyId = null)
        {
            var query = _db.Products
                .Include(p => p.Categories)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .AsQueryable();

            if (companyId.HasValue)
            {
                query = query.Where(p => p.CompanyId == companyId.Value);
            }

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
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (product is null) return NotFound();
            
            return Ok(MapToDto(product));
        }

        [HttpGet("by-category/{categoryId:int}")]
        public async Task<ActionResult<PaginatedResult<ProductDto>>> GetByCategory(int categoryId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 9, [FromQuery] int? companyId = null)
        {
            var query = _db.Products
                .Include(p => p.Categories)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .Where(p => p.Categories.Any(c => c.Id == categoryId))
                .AsQueryable();

            if (companyId.HasValue)
            {
                query = query.Where(p => p.CompanyId == companyId.Value);
            }

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

        [HttpGet("by-cluster/{clusterId:int}")]
        public async Task<ActionResult<PaginatedResult<ProductDto>>> GetByCluster(int clusterId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 9, [FromQuery] int? companyId = null)
        {
            var query = _db.Products
                .Include(p => p.Categories)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .Where(p => _db.ProductClusterMembers.Any(m => m.ProductId == p.Id && m.ProductClusterId == clusterId))
                .AsQueryable();

            if (companyId.HasValue)
            {
                query = query.Where(p => p.CompanyId == companyId.Value);
            }

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
                BrandId = product.BrandId,
                BrandName = product.Brand?.Name,
                Categories = product.Categories.Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    ParentId = c.ParentId
                    // Children mapping is skipped here to avoid potential circular depth issues in simple product view
                }).ToList(),
                Variants = product.Variants.Select(v => new VariantProductDto
                {
                    Id = v.Id,
                    ProductId = v.ProductId,
                    ProductName = product.Name,
                    Color = v.Color,
                    Price = v.Price,
                    CoverImage = v.CoverImage,
                    IsCustomizable = v.IsCustomizable,
                    BrandName = product.Brand?.Name,
                    CategoryNames = product.Categories.Select(c => c.Name).ToList(),
                    Quantity = product.Quantity,
                    Images = product.Images
                }).ToList()
            };
        }
    }
}
