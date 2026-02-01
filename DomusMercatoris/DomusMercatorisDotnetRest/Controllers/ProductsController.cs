using Microsoft.AspNetCore.Mvc;
using DomusMercatoris.Data;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Service.DTOs;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using DomusMercatorisDotnetRest.Infrastructure;
using DomusMercatorisDotnetRest.Services;

namespace DomusMercatorisDotnetRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly DomusDbContext _db;
        private readonly IMapper _mapper;
        private readonly ProductService _productService;

        public ProductsController(DomusDbContext db, IMapper mapper, ProductService productService)
        {
            _db = db;
            _mapper = mapper;
            _productService = productService;
        }

        private IQueryable<Product> BaseProductQuery() => ProductQueryHelper.BaseProductQuery(_db);
        private IQueryable<Product> ApplyCompanyFilter(IQueryable<Product> query, int? companyId) => ProductQueryHelper.ApplyCompanyFilter(query, companyId);
        private Task<PaginatedResult<ProductDto>> PaginateAndMapAsync(IQueryable<Product> query, int pageNumber, int pageSize) => ProductQueryHelper.PaginateAndMapAsync(query, pageNumber, pageSize, _mapper);

        [HttpGet]
        public async Task<ActionResult<PaginatedResult<ProductDto>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 9, [FromQuery] int? companyId = null, [FromQuery] int? brandId = null)
        {
            var result = await _productService.GetAllAsync(pageNumber, pageSize, companyId, brandId);
            return Ok(result);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<ProductDto>> GetById(long id)
        {
            var dto = await _productService.GetByIdAsync(id);
            if (dto is null) return NotFound();
            return Ok(dto);
        }

        [HttpGet("by-category/{categoryId:int}")]
        public async Task<ActionResult<PaginatedResult<ProductDto>>> GetByCategory(int categoryId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 9, [FromQuery] int? companyId = null, [FromQuery] int? brandId = null)
        {
            var result = await _productService.GetByCategoryAsync(categoryId, pageNumber, pageSize, companyId, brandId);
            return Ok(result);
        }

        [HttpGet("by-cluster/{clusterId:int}")]
        public async Task<ActionResult<PaginatedResult<ProductDto>>> GetByCluster(
            int clusterId, 
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 9, 
            [FromQuery] int? companyId = null, 
            [FromQuery] int? brandId = null,
            [FromQuery] List<long>? prioritizedIds = null)
        {
            var result = await _productService.GetByClusterAsync(clusterId, pageNumber, pageSize, companyId, brandId, prioritizedIds);
            return Ok(result);
        }

        /// <summary>
        /// Search products by query text in name, SKU, or description.
        /// </summary>
        /// <param name="query">Text to search (required)</param>
        /// <param name="pageNumber">Page number (default 1)</param>
        /// <param name="pageSize">Page size (default 9)</param>
        /// <param name="companyId">Optional company filter</param>
        /// <param name="brandId">Optional brand filter</param>
        /// <param name="autoCategoryId">Optional auto category filter</param>
        [HttpGet("search")]
        [ProducesResponseType(typeof(PaginatedResult<ProductDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResult<ProductDto>>> Search(
            [FromQuery] string query,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 9,
            [FromQuery] int? companyId = null,
            [FromQuery] int? brandId = null,
            [FromQuery] int? autoCategoryId = null)
        {
            var q = (query ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(q))
            {
                return Ok(new PaginatedResult<ProductDto>
                {
                    Items = new List<ProductDto>(),
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = 0
                });
            }

            q = q.ToLower();

            var result = await _productService.SearchAsync(q, pageNumber, pageSize, companyId, brandId, autoCategoryId);
            return Ok(result);
        }

        // Manual mapper removed in favor of AutoMapper profiles
    }
}
