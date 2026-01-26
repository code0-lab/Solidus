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
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using System.IO;

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

            user.FirstName = dto.FirstName ?? user.FirstName;
            user.LastName = dto.LastName ?? user.LastName;
            user.Phone = dto.Phone;
            user.Address = dto.Address;

            // Handle Email Change
            if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(dto.CurrentPassword))
                {
                    // If email is changing, current password is required
                    // In a real app, you might throw a specific exception or return a result object
                    // For now, we'll return null to indicate failure (controller can handle basic validation)
                    // But to be more specific, we might need a richer return type. 
                    // Let's assume the controller validated that if Email != CurrentEmail, Password is present.
                    // But here we must validate the password is CORRECT.
                    return null; 
                }

                var hashedCurrent = HashSha256(dto.CurrentPassword);
                if (user.Password != hashedCurrent) return null; // Incorrect password

                // Check uniqueness
                if (await _db.Users.AnyAsync(u => u.Email == dto.Email && u.Id != id))
                    return null; // Email taken

                user.Email = dto.Email;
            }

            await _db.SaveChangesAsync();
            return _mapper.Map<UserDto>(user);
        }

        public async Task<bool> ChangePasswordAsync(long userId, ChangePasswordDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            var hashedCurrent = HashSha256(dto.CurrentPassword);
            if (user.Password != hashedCurrent) return false;

            user.Password = HashSha256(dto.NewPassword);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangeEmailAsync(long userId, ChangeEmailDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            var hashedCurrent = HashSha256(dto.CurrentPassword);
            if (user.Password != hashedCurrent) return false;

            if (await _db.Users.AnyAsync(u => u.Email.ToLower() == dto.NewEmail.ToLower() && u.Id != userId))
            {
                return false;
            }

            user.Email = dto.NewEmail;
            await _db.SaveChangesAsync();
            return true;
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

        public async Task<UserDto?> UploadProfilePictureAsync(long userId, IFormFile file)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null) return null;

            // Regex validation for checks
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!Regex.IsMatch(extension, @"^\.(jpg|jpeg|png|webp)$"))
            {
                return null; // Invalid format
            }

            // Define upload path: ../MVC/MVC/wwwroot/uploads
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "..", "MVC", "MVC", "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + extension;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Save relative URL
            user.ProfilePictureUrl = $"/uploads/{uniqueFileName}";

            await _db.SaveChangesAsync();
            return _mapper.Map<UserDto>(user);
        }
    }
}
