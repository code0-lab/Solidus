using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DomusMercatoris.Data;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Service.DTOs;

namespace DomusMercatorisDotnetRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Moderator")]
    public class ModeratorController : ControllerBase
    {
        private readonly DomusDbContext _db;

        public ModeratorController(DomusDbContext db)
        {
            _db = db;
        }

        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers([FromQuery] string? search)
        {
            var query = _db.Users.Include(u => u.Ban).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower().Trim();
                query = query.Where(u => 
                    u.Email.ToLower().Contains(s) || 
                    u.FirstName.ToLower().Contains(s) || 
                    u.LastName.ToLower().Contains(s));
            }

            var users = await query.OrderBy(u => u.Email).Take(50).ToListAsync();
            return Ok(users.Select(MapToDto));
        }

        [HttpPost("ban")]
        public async Task<ActionResult> BanUser([FromBody] BanUserRequestDto dto)
        {
            var user = await _db.Users.Include(u => u.Ban).FirstOrDefaultAsync(u => u.Id == dto.UserId);
            if (user == null) return NotFound("User not found");

            if (user.Ban == null)
            {
                user.Ban = new Ban
                {
                    UserId = user.Id
                };
            }

            user.Ban.PermaBan = dto.PermaBan;
            user.Ban.EndDate = dto.EndDate;
            user.Ban.Reason = dto.Reason;
            user.Ban.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(new { message = "User banned successfully" });
        }

        [HttpPost("unban/{userId:long}")]
        public async Task<ActionResult> UnbanUser(long userId)
        {
            var user = await _db.Users.Include(u => u.Ban).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound("User not found");

            if (user.Ban != null)
            {
                _db.Bans.Remove(user.Ban);
                await _db.SaveChangesAsync();
            }

            return Ok(new { message = "User unbanned successfully" });
        }

        private static UserDto MapToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                CompanyId = user.CompanyId,
                Roles = user.Roles,
                Ban = user.Ban == null ? null : new BanDto
                {
                    IsBaned = user.Ban.IsBaned,
                    PermaBan = user.Ban.PermaBan,
                    EndDate = user.Ban.EndDate,
                    Reason = user.Ban.Reason,
                    ObjectToBan = user.Ban.ObjectToBan,
                    Object = user.Ban.Object
                }
            };
        }
    }
}
