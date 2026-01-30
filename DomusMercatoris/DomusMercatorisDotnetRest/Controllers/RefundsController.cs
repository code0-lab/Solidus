using DomusMercatoris.Service.DTOs;
using DomusMercatoris.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DomusMercatorisDotnetRest.Controllers
{
    /// <summary>
    /// Handles refund requests and their status retrieval.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RefundsController : ControllerBase
    {
        private readonly RefundService _refundService;

        public RefundsController(RefundService refundService)
        {
            _refundService = refundService;
        }

        /// <summary>
        /// Creates a new refund request for a specific order item.
        /// </summary>
        /// <param name="dto">The refund request details.</param>
        /// <returns>Ok if successful, BadRequest otherwise.</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRefundRequestDto dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var success = await _refundService.CreateRefundRequestAsync(userId, dto);
            if (!success)
                return BadRequest("Invalid refund request.");

            return Ok();
        }

        /// <summary>
        /// Retrieves all refund requests for the authenticated user.
        /// </summary>
        /// <returns>A list of refund requests.</returns>
        [HttpGet("my")]
        public async Task<IActionResult> GetMyRefunds()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var refunds = await _refundService.GetUserRefundsAsync(userId);
            return Ok(refunds);
        }
    }
}
