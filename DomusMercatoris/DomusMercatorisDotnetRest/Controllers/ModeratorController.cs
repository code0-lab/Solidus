using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DomusMercatoris.Service.DTOs;
using DomusMercatorisDotnetRest.Services;
using DomusMercatoris.Core.Constants;

namespace DomusMercatorisDotnetRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Moderator,Rex")]
    public class ModeratorController : ControllerBase
    {
        private readonly ModeratorService _moderatorService;

        public ModeratorController(ModeratorService moderatorService)
        {
            _moderatorService = moderatorService;
        }

        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers([FromQuery] string? search)
        {
            var users = await _moderatorService.GetUsersAsync(search);
            return Ok(users);
        }

        [HttpPost("ban")]
        public async Task<ActionResult> BanUser([FromBody] BanUserRequestDto dto)
        {
            var result = await _moderatorService.BanUserAsync(dto);
            if (!result.Success) return BadRequest(new { message = result.Message });
            return Ok(new { message = result.Message });
        }

        [HttpPost("unban/{userId:long}")]
        public async Task<ActionResult> UnbanUser(long userId)
        {
            var ok = await _moderatorService.UnbanUserAsync(userId);
            if (!ok) return NotFound("User not found");
            return Ok(new { message = "User unbanned successfully" });
        }
        // Manual mapping removed in favor of AutoMapper profiles
    }
}
