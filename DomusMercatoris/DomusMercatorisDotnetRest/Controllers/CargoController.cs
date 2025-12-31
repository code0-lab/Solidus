using DomusMercatoris.Service.DTOs;
using DomusMercatoris.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DomusMercatorisDotnetRest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CargoController : ControllerBase
    {
        private readonly CargoService _cargoService;

        public CargoController(CargoService cargoService)
        {
            _cargoService = cargoService;
        }

        [HttpGet("{trackingNumber}")]
        public async Task<IActionResult> GetByTrackingNumber(string trackingNumber)
        {
            var cargo = await _cargoService.GetByTrackingNumberAsync(trackingNumber);
            if (cargo == null)
            {
                return NotFound("Cargo not found.");
            }
            return Ok(cargo);
        }

        [HttpGet("my-cargos")]
        [Authorize]
        public async Task<IActionResult> GetMyCargos()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out long userId))
            {
                return Unauthorized();
            }

            var cargos = await _cargoService.GetUserCargosAsync(userId);
            return Ok(cargos);
        }

        [HttpPost]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> Create([FromBody] CreateCargoTrackingDto dto)
        {
            var cargo = await _cargoService.CreateTrackingAsync(dto);
            return CreatedAtAction(nameof(GetByTrackingNumber), new { trackingNumber = cargo.TrackingNumber }, cargo);
        }

        [HttpPut("status")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateCargoStatusDto dto)
        {
            var result = await _cargoService.UpdateStatusAsync(dto);
            if (!result)
            {
                return NotFound("Cargo not found.");
            }
            return Ok("Status updated successfully.");
        }
    }
}