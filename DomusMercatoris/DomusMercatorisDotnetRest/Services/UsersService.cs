using AutoMapper;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DomusMercatorisDotnetRest.Services
{
    public class UsersService
    {
        private readonly DomusDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;

        public UsersService(DomusDbContext db, IConfiguration configuration, IMapper mapper)
        {
            _db = db;
            _configuration = configuration;
            _mapper = mapper;
        }

        public async Task<LoginResponseDto?> LoginAsync(UserLoginDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());
            if (user is null) return null;
            var hashedPassword = HashSha256(dto.Password);
            if (user.Password != hashedPassword) return null;
            var token = GenerateJwtToken(user);
            return new LoginResponseDto
            {
                Token = token,
                User = _mapper.Map<UserDto>(user)
            };
        }

        public async Task<UserDto?> RegisterAsync(UserRegisterDto dto)
        {
            if (await _db.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower()))
            {
                return null;
            }
            var companyExists = await _db.Companies.AnyAsync(c => c.CompanyId == dto.CompanyId);
            if (!companyExists) return null;
            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Password = HashSha256(dto.Password),
                CompanyId = dto.CompanyId,
                Roles = new List<string> { "customer" },
                CreatedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto?> GetByIdAsync(long id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            return user is null ? null : _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto?> UpdateProfileAsync(long id, UpdateUserProfileDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user is null) return null;

            user.Phone = dto.Phone;
            user.Address = dto.Address;

            await _db.SaveChangesAsync();
            return _mapper.Map<UserDto>(user);
        }

        private string HashSha256(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            var builder = new StringBuilder();
            foreach (var b in bytes) builder.Append(b.ToString("x2"));
            return builder.ToString();
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("CompanyId", user.CompanyId.ToString())
            };
            foreach (var role in user.Roles) claims.Add(new Claim(ClaimTypes.Role, role));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
