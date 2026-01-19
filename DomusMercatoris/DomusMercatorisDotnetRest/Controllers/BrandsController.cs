using Microsoft.AspNetCore.Mvc;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace DomusMercatorisDotnetRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BrandsController : ControllerBase
    {
        private readonly DomusDbContext _db;
        private readonly IMapper _mapper;

        public BrandsController(DomusDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<BrandDto>>> GetAll([FromQuery] int? companyId = null)
        {
            var query = _db.Brands.AsQueryable();

            if (companyId.HasValue)
            {
                query = query.Where(b => b.CompanyId == companyId.Value);
            }

            var brands = await query.OrderBy(b => b.Name).ToListAsync();
            return Ok(_mapper.Map<List<BrandDto>>(brands));
        }
    }
}
