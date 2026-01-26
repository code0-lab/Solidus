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

        /// <summary>
        /// Updates the current user's profile (Phone, Address).
        /// </summary>
        /// <param name="dto">Profile update information</param>
        /// <returns>Updated user profile</returns>
        /// <response code="200">Returns the updated user profile</response>
        /// <response code="400">If the input is invalid</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="404">If the user is not found</response>
        [HttpPut("me")]
        [Authorize]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> UpdateMe([FromBody] UpdateUserProfileDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var updated = await _usersService.UpdateProfileAsync(userId.Value, dto);
            if (updated is null) return NotFound();

            return Ok(updated);
        }

        [HttpPost("me/picture")]
        [Authorize]
        public async Task<ActionResult<UserDto>> UploadProfilePicture(IFormFile file)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            if (file == null || file.Length == 0)
                return BadRequest("Dosya yüklenmedi.");

            var updated = await _usersService.UploadProfilePictureAsync(userId.Value, file);
            if (updated is null) return BadRequest("Geçersiz dosya formatı veya kullanıcı bulunamadı.");

            return Ok(updated);
        }

        /// <summary>
        /// Changes the current user's password.
        /// </summary>
        /// <param name="dto">Password change information containing current and new password</param>
        /// <returns>Success message</returns>
        /// <response code="200">If password was changed successfully</response>
        /// <response code="400">If current password is incorrect or input is invalid</response>
        /// <response code="401">If the user is not authenticated</response>
        [HttpPut("me/password")]
        [Authorize]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdatePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var success = await _usersService.ChangePasswordAsync(userId.Value, dto);
            if (!success) return BadRequest(new { message = "Şifre değiştirilemedi. Mevcut şifrenizi kontrol edin." });

            return Ok(new { message = "Şifre başarıyla güncellendi." });
        }

        /// <summary>
        /// Changes the current user's email address.
        /// </summary>
        /// <param name="dto">Email change information containing new email and current password for verification</param>
        /// <returns>Success message</returns>
        /// <response code="200">If email was changed successfully</response>
        /// <response code="400">If password is incorrect, email is already taken, or input is invalid</response>
        /// <response code="401">If the user is not authenticated</response>
        [HttpPut("me/email")]
        [Authorize]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateEmail([FromBody] ChangeEmailDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var success = await _usersService.ChangeEmailAsync(userId.Value, dto);
            if (!success) return BadRequest(new { message = "Email değiştirilemedi. Şifrenizi kontrol edin veya email kullanımda." });

            return Ok(new { message = "Email başarıyla güncellendi." });
        }
    }
}
