using Microsoft.AspNetCore.Mvc;
using DomusMercatoris.Service.DTOs;
using Microsoft.AspNetCore.Http;
using DomusMercatorisDotnetRest.Services;
using DomusMercatoris.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DomusMercatoris.Core.Exceptions;

namespace DomusMercatorisDotnetRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly OrdersService _ordersService;
        public OrdersController(OrdersService ordersService)
        {
            _ordersService = ordersService;
        }

        [HttpPost("checkout")]
        [Authorize]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<OrderDto>> Checkout([FromBody] OrderCreateDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            // Security check: Force UserId to match the authenticated user
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            dto.UserId = userId;
            dto.FleetingUser = null; // Ensure no fleeting user is created if logged in

            var result = await _ordersService.CheckoutAsync(dto);
            return Ok(result);
        }

        [HttpPost("{id:long}/mark-paid")]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderDto>> MarkPaid(long id)
        {
            var res = await _ordersService.MarkPaidAsync(id);
            return Ok(res);
        }

        [HttpGet("{id:long}")]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderDto>> Get(long id)
        {
            var res = await _ordersService.GetAsync(id);
            return Ok(res);
        }

        /// <summary>
        /// Retrieves successful orders for the current user.
        /// </summary>
        [HttpGet("my-orders")]
        [Authorize]
        [ProducesResponseType(typeof(PaginatedResult<OrderDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResult<OrderDto>>> GetSuccessfulOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var res = await _ordersService.GetByUserIdAsync(userId.Value, page, pageSize, "orders");
            return Ok(res);
        }

        /// <summary>
        /// Retrieves failed orders for the current user.
        /// </summary>
        [HttpGet("my-orders/failed")]
        [Authorize]
        [ProducesResponseType(typeof(PaginatedResult<OrderDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResult<OrderDto>>> GetFailedOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var res = await _ordersService.GetByUserIdAsync(userId.Value, page, pageSize, "failed-orders");
            return Ok(res);
        }

        [HttpGet("{id:long}/tracking")]
        [ProducesResponseType(typeof(CargoTracking), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CargoTracking>> GetTracking(long id)
        {
            var tr = await _ordersService.GetTrackingAsync(id);
            return Ok(tr);
        }

        private long? GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            return null;
        }
    }
}
