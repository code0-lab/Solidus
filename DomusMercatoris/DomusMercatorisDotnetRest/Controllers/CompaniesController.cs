using Microsoft.AspNetCore.Mvc;
using DomusMercatoris.Data;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Service.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DomusMercatorisDotnetRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompaniesController : ControllerBase
    {
        private readonly DomusDbContext _db;

        public CompaniesController(DomusDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CompanyDto>>> GetAll()
        {
            // Auto-seed if empty (for convenience in dev)
            if (!await _db.Companies.AnyAsync())
            {
                var defaultCompany = new Company
                {
                    Name = "Domus Mercatoris",
                    CreatedAt = DateTime.UtcNow
                };
                _db.Companies.Add(defaultCompany);
                await _db.SaveChangesAsync();
            }

            var companies = await _db.Companies
                .OrderBy(c => c.Name)
                .Select(c => new CompanyDto
                {
                    CompanyId = c.CompanyId,
                    Name = c.Name
                })
                .ToListAsync();

            return Ok(companies);
        }
    }
}
