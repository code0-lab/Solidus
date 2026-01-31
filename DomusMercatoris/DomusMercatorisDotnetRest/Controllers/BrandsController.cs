using Microsoft.AspNetCore.Mvc;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using DomusMercatoris.Service.Interfaces;

namespace DomusMercatorisDotnetRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BrandsController : ControllerBase
    {
        private readonly DomusDbContext _db;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public BrandsController(DomusDbContext db, IMapper mapper, ICurrentUserService currentUserService)
        {
            _db = db;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<ActionResult<List<BrandDto>>> GetAll([FromQuery] int? companyId = null)
        {
            var query = _db.Brands.AsQueryable();

            if (_currentUserService.CompanyId.HasValue)
            {
                companyId = _currentUserService.CompanyId.Value;
            }

            if (companyId.HasValue)
            {
                query = query.Where(b => b.CompanyId == companyId.Value);
            }

            var brands = await query.OrderBy(b => b.Name).ToListAsync();
            return Ok(_mapper.Map<List<BrandDto>>(brands));
        }
    }
}
