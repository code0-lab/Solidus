using Microsoft.AspNetCore.Mvc;
using DomusMercatoris.Service.DTOs;
using System.Security.Claims;
using DomusMercatorisDotnetRest.Services;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;

namespace DomusMercatorisDotnetRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UsersService _usersService;

        public UsersController(UsersService usersService)
        {
            _usersService = usersService;
        }

        private long? GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                               ?? User.FindFirst("sub")
                               ?? User.FindFirst(JwtRegisteredClaimNames.Sub)
                               ?? User.FindFirst("id");

            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId))
            {
                return userId;
            }
            return null;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] UserLoginDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var res = await _usersService.LoginAsync(dto);
            if (res == null) return Unauthorized(new { message = "Geçersiz email veya şifre" });
            return Ok(res);
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register([FromBody] UserRegisterDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userDto = await _usersService.RegisterAsync(dto);
            if (userDto == null) return Conflict(new { message = "Email zaten kayıtlı veya geçersiz CompanyId" });
            return CreatedAtAction(nameof(GetById), new { id = userDto.Id }, userDto);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<UserDto>> GetById(long id)
        {
            var user = await _usersService.GetByIdAsync(id);
            if (user is null) return NotFound();
            return Ok(user);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserDto>> Me()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var user = await _usersService.GetByIdAsync(userId.Value);
            if (user is null) return NotFound();

            return Ok(user);
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<ActionResult<UserDto>> UpdateMe([FromBody] UpdateUserProfileDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var updated = await _usersService.UpdateProfileAsync(userId.Value, dto);
            if (updated is null) return NotFound();

            return Ok(updated);
        }
    }
}
