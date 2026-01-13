using Microsoft.AspNetCore.Mvc;
using DomusMercatoris.Service.DTOs;
using Microsoft.AspNetCore.Http;
using DomusMercatorisDotnetRest.Services;
using DomusMercatoris.Core.Entities;
namespace DomusMercatorisDotnetRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesController : ControllerBase
    {
        private readonly SalesService _salesService;
        public SalesController(SalesService salesService)
        {
            _salesService = salesService;
        }
        [HttpPost("checkout")]
        [ProducesResponseType(typeof(SaleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SaleDto>> Checkout([FromBody] SaleCreateDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var result = await _salesService.CheckoutAsync(dto);
            return Ok(result);
        }
        [HttpPost("{id:long}/mark-paid")]
        [ProducesResponseType(typeof(SaleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SaleDto>> MarkPaid(long id)
        {
            var res = await _salesService.MarkPaidAsync(id);
            if (res == null) return NotFound();
            return Ok(res);
        }
        [HttpGet("{id:long}")]
        [ProducesResponseType(typeof(SaleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SaleDto>> Get(long id)
        {
            var res = await _salesService.GetAsync(id);
            if (res == null) return NotFound();
            return Ok(res);
        }
        [HttpGet("{id:long}/tracking")]
        [ProducesResponseType(typeof(CargoTracking), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CargoTracking>> GetTracking(long id)
        {
            var tr = await _salesService.GetTrackingAsync(id);
            if (tr == null) return NotFound();
            return Ok(tr);
        }
    }
}
