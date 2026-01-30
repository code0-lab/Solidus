using DomusMercatoris.Service.DTOs;
using DomusMercatoris.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DomusMercatorisDotnetRest.Controllers
{
    /// <summary>
    /// Manages cargo tracking and shipping information.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CargoController : ControllerBase
    {
        private readonly CargoService _cargoService;

        public CargoController(CargoService cargoService)
        {
            _cargoService = cargoService;
        }

        /// <summary>
        /// Retrieves cargo tracking details by tracking number.
        /// </summary>
        /// <param name="trackingNumber">The unique tracking number.</param>
        /// <returns>Cargo tracking details.</returns>
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

        /// <summary>
        /// Retrieves all cargo shipments associated with the current user.
        /// </summary>
        /// <returns>List of user's cargo shipments.</returns>
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

        /// <summary>
        /// Creates a new cargo tracking entry (Admin/Manager only).
        /// </summary>
        /// <param name="dto">Cargo creation details.</param>
        /// <returns>Created cargo details.</returns>
        [HttpPost]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> Create([FromBody] CreateCargoTrackingDto dto)
        {
            var cargo = await _cargoService.CreateTrackingAsync(dto);
            return CreatedAtAction(nameof(GetByTrackingNumber), new { trackingNumber = cargo.TrackingNumber }, cargo);
        }

        /// <summary>
        /// Updates the status of an existing shipment (Admin/Manager only).
        /// </summary>
        /// <param name="dto">Status update details.</param>
        /// <returns>Success message.</returns>
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