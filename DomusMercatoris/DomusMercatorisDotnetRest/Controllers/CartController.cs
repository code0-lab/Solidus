using System.Security.Claims;
using DomusMercatoris.Service.DTOs;
using DomusMercatoris.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DomusMercatorisDotnetRest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly CartService _cartService;

        public CartController(CartService cartService)
        {
            _cartService = cartService;
        }

        private long GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            throw new UnauthorizedAccessException("User ID not found in token");
        }

        [HttpGet]
        public async Task<ActionResult<List<CartItemDto>>> GetCart()
        {
            return Ok(await _cartService.GetCartAsync(GetUserId()));
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            await _cartService.AddToCartAsync(GetUserId(), dto);
            return Ok();
        }

        [HttpPatch("{itemId}")]
        public async Task<IActionResult> UpdateQuantity(long itemId, [FromBody] UpdateCartItemDto dto)
        {
            await _cartService.UpdateQuantityAsync(GetUserId(), itemId, dto.Quantity);
            return Ok();
        }

        [HttpDelete("{itemId}")]
        public async Task<IActionResult> RemoveFromCart(long itemId)
        {
            await _cartService.RemoveFromCartAsync(GetUserId(), itemId);
            return Ok();
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncCart([FromBody] List<SyncCartItemDto> items)
        {
            await _cartService.SyncCartAsync(GetUserId(), items);
            return Ok();
        }

        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            await _cartService.ClearCartAsync(GetUserId());
            return Ok();
        }
    }
}