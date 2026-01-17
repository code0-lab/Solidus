using Microsoft.AspNetCore.Mvc;
using DomusMercatoris.Service.DTOs;
using DomusMercatoris.Service.Services;

namespace DomusMercatorisDotnetRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BannersController : ControllerBase
    {
        private readonly BannerService _bannerService;

        public BannersController(BannerService bannerService)
        {
            _bannerService = bannerService;
        }

        [HttpGet("active/{companyId:int}")]
        public async Task<ActionResult<BannerDto?>> GetActiveByCompany(int companyId)
        {
            var banner = await _bannerService.GetActiveBannerAsync(companyId);
            if (banner == null) return NotFound();
            return Ok(banner);
        }

        [HttpGet("active")]
        public async Task<ActionResult<BannerDto?>> GetActive()
        {
            var banner = await _bannerService.GetAnyActiveBannerAsync();
            if (banner == null) return NotFound();
            return Ok(banner);
        }
    }
}
