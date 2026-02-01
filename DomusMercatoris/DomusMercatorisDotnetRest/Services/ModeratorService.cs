using AutoMapper;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using DomusMercatoris.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DomusMercatorisDotnetRest.Services
{
    public class ModeratorService
    {
        private readonly DomusDbContext _db;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public ModeratorService(DomusDbContext db, IMapper mapper, ICurrentUserService currentUserService)
        {
            _db = db;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<List<UserDto>> GetUsersAsync(string? search)
        {
            var query = _db.Users.Include(u => u.Ban).AsQueryable();

            if (_currentUserService.CompanyId.HasValue)
            {
                query = query.Where(u => u.CompanyId == _currentUserService.CompanyId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower().Trim();
                query = query.Where(u =>
                    (u.Email ?? string.Empty).ToLower().Contains(s) ||
                    (u.FirstName ?? string.Empty).ToLower().Contains(s) ||
                    (u.LastName ?? string.Empty).ToLower().Contains(s));
            }
            var users = await query.OrderBy(u => u.Email).Take(50).ToListAsync();
            return _mapper.Map<List<UserDto>>(users);
        }

        public async Task<(bool Success, string Message)> BanUserAsync(BanUserRequestDto dto)
        {
            var query = _db.Users.Include(u => u.Ban).AsQueryable();

            if (_currentUserService.CompanyId.HasValue)
            {
                query = query.Where(u => u.CompanyId == _currentUserService.CompanyId.Value);
            }

            var user = await query.FirstOrDefaultAsync(u => u.Id == dto.UserId);
            if (user == null) return (false, "User not found");
            
            // Check if target user is Rex
            if (user.Roles != null && user.Roles.Contains("Rex", StringComparer.OrdinalIgnoreCase))
            {
                return (false, "Security Alert: You cannot ban a Rex user.");
            }

            if (user.Ban == null)
            {
                user.Ban = new Ban { UserId = user.Id };
            }
            user.Ban.PermaBan = dto.PermaBan;
            user.Ban.EndDate = dto.EndDate;
            user.Ban.Reason = dto.Reason;
            user.Ban.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return (true, "User banned successfully");
        }

        public async Task<bool> UnbanUserAsync(long userId)
        {
            var query = _db.Users.Include(u => u.Ban).AsQueryable();

            if (_currentUserService.CompanyId.HasValue)
            {
                query = query.Where(u => u.CompanyId == _currentUserService.CompanyId.Value);
            }

            var user = await query.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;
            if (user.Ban != null)
            {
                _db.Bans.Remove(user.Ban);
                await _db.SaveChangesAsync();
            }
            return true;
        }
    }
}
