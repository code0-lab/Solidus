using Microsoft.AspNetCore.Mvc;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DomusMercatorisDotnetRest.Services;

namespace DomusMercatorisDotnetRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly DomusDbContext _db;
        private readonly UsersService _usersService;

        public UsersController(DomusDbContext db, UsersService usersService)
        {
            _db = db;
            _usersService = usersService;
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
        // Manual mapping and token/hash utilities moved to UsersService
    }
}
