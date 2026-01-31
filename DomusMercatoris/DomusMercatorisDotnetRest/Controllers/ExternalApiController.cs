using Microsoft.AspNetCore.Mvc;
using DomusMercatoris.Service.DTOs;
using DomusMercatorisDotnetRest.Authentication;
using DomusMercatorisDotnetRest.Services;
using DomusMercatoris.Service.Services; // For BrandService, OrderService
using DomusMercatoris.Service.Interfaces;
using AutoMapper;
using System.Collections.Generic;

namespace DomusMercatorisDotnetRest.Controllers
{
    [ApiController]
    [Route("api/external")]
    [ApiKey] // Enforces API Key authentication for all endpoints in this controller
    public class ExternalApiController : ControllerBase
    {
        private readonly ProductService _productService;
        private readonly CategoryService _categoryService;
        private readonly BrandService _brandService;
        private readonly OrderService _orderService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public ExternalApiController(
            ProductService productService,
            CategoryService categoryService,
            BrandService brandService,
            OrderService orderService,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _productService = productService;
            _categoryService = categoryService;
            _brandService = brandService;
            _orderService = orderService;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        /// <summary>
        /// Get all products (API Key protected)
        /// </summary>
        [HttpGet("products")]
        public async Task<ActionResult<PaginatedResult<ProductDto>>> GetProducts(
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 9, 
            [FromQuery] int? brandId = null)
        {
            // CompanyId is automatically handled by the service via ICurrentUserService or we can pass it explicitly
            // ProductService.GetAllAsync takes companyId as optional. 
            // Since API Key enforces Company scope, we should pass it or let service handle it.
            // Based on ApiKeyAuthenticationHandler, CompanyId claim is set.
            
            var result = await _productService.GetAllAsync(pageNumber, pageSize, _currentUserService.CompanyId, brandId);
            return Ok(result);
        }

        /// <summary>
        /// Get all categories
        /// </summary>
        [HttpGet("categories")]
        public async Task<ActionResult<List<CategoryDto>>> GetCategories()
        {
            var result = await _categoryService.GetAllAsync();
            return Ok(result);
        }

        /// <summary>
        /// Get all brands
        /// </summary>
        [HttpGet("brands")]
        public async Task<ActionResult<List<BrandDto>>> GetBrands()
        {
            var companyId = _currentUserService.CompanyId;
            if (!companyId.HasValue) return Unauthorized(new { message = "Company context not found in API Key" });

            var result = await _brandService.GetBrandsByCompanyAsync(companyId.Value);
            return Ok(result);
        }

        /// <summary>
        /// Get orders
        /// </summary>
        [HttpGet("orders")]
        public async Task<ActionResult<PaginatedResult<OrderDto>>> GetOrders(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string tab = "active")
        {
            var companyId = _currentUserService.CompanyId;
            if (!companyId.HasValue) return Unauthorized(new { message = "Company context not found in API Key" });

            var (items, totalCount) = await _orderService.GetPagedByCompanyIdAsync(companyId.Value, pageNumber, pageSize, tab);
            
            var dtos = _mapper.Map<List<OrderDto>>(items);

            return Ok(new PaginatedResult<OrderDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }

        /// <summary>
        /// Check API Key validity
        /// </summary>
        [HttpGet("check")]
        public IActionResult Check()
        {
            return Ok(new { message = "API Key is valid", user = User.Identity?.Name, claims = User.Claims.Select(c => new { c.Type, c.Value }) });
        }
    }
}
