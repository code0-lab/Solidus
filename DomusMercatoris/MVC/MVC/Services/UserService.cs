using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using DomusMercatorisDotnetMVC.Dto.UserDto;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using DomusMercatoris.Service.Services;

namespace DomusMercatorisDotnetMVC.Services
{
    public class UserService
    {

        private readonly DomusDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly EncryptionService _encryptionService;

        public UserService(DomusDbContext dbContext, IMapper mapper, EncryptionService encryptionService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _encryptionService = encryptionService;
        }

        public async Task<User?> UserLoginAsync(string Email, string Password)
        {
            var user = await _dbContext.Users.Include(u => u.Ban).SingleOrDefaultAsync(u => u.Email == Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(Password, user.Password))
            {
                return null;
            }
            return user;
        }

        public async Task<User> UserRegisterAsync(UserRegisterDto userRegisterDto)
        {
            var userEntity = _mapper.Map<User>(userRegisterDto);
            userEntity.Password = BCrypt.Net.BCrypt.HashPassword(userRegisterDto.Password);
            userEntity.Roles = new List<string> { "Manager", "User", "Customer" };

            var companyName = (userRegisterDto.CompanyName ?? string.Empty).Trim();
            var company = new Company { Name = companyName };
            _dbContext.Companies.Add(company);
            await _dbContext.SaveChangesAsync();

            userEntity.CompanyId = company.CompanyId;
            _dbContext.Users.Add(userEntity);
            await _dbContext.SaveChangesAsync();
            return userEntity;
        }

        public async Task<User> RegisterWorkerAsync(UserRegisterDto userRegisterDto, int companyId)
        {
            var userEntity = _mapper.Map<User>(userRegisterDto);
            userEntity.Password = BCrypt.Net.BCrypt.HashPassword(userRegisterDto.Password);
            userEntity.Roles = new List<string> { "User" };
            userEntity.CompanyId = companyId;
            _dbContext.Users.Add(userEntity);
            await _dbContext.SaveChangesAsync();
            return userEntity;
        }

        public async Task<User?> GetByIdAsync(long id)
        {
            return await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _dbContext.Users
                .Include(u => u.Company)
                .SingleOrDefaultAsync(u => u.Email == email);
        }

        public async Task<List<User>> GetByCompanyAsync(int companyId)
        {
            return await _dbContext.Users.Where(u => u.CompanyId == companyId).OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToListAsync();
        }

        public async Task<List<User>> SearchByCompanyAsync(int companyId, string query, int limit = 20)
        {
            var q = (query ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(q)) return new List<User>();
            q = q.ToLowerInvariant();
            return await _dbContext.Users
                .Where(u => u.CompanyId == companyId && (
                    (u.FirstName ?? string.Empty).ToLower().Contains(q) ||
                    (u.LastName ?? string.Empty).ToLower().Contains(q) ||
                    (u.Email ?? string.Empty).ToLower().Contains(q)
                ))
                .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<bool> UpdateUserInCompanyAsync(long id, int companyId, string firstName, string lastName, string email)
        {
            var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId);
            if (user == null)
            {
                return false;
            }
            var existsEmail = await _dbContext.Users.AnyAsync(u => u.Email == email && u.Id != id);
            if (existsEmail)
            {
                return false;
            }
            user.FirstName = firstName;
            user.LastName = lastName;
            user.Email = email;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserInCompanyAsync(long id, int companyId)
        {
            var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId);
            if (user == null)
            {
                return false;
            }

            // Security Check: Prevent deletion of Rex or Moderator
            if ((user.Roles ?? new List<string>()).Any(r => r == "Rex" || r == "Moderator"))
            {
                return false;
            }

            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<string?> GetCompanyNameAsync(int companyId)
        {
            var company = await _dbContext.Companies.SingleOrDefaultAsync(c => c.CompanyId == companyId);
            return company?.Name;
        }

        public async Task<List<Company>> GetAllCompaniesAsync()
        {
            return await _dbContext.Companies
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<string?> GetCompanyGeminiKeyAsync(int companyId)
        {
            var company = await _dbContext.Companies.SingleOrDefaultAsync(c => c.CompanyId == companyId);
            if (string.IsNullOrEmpty(company?.GeminiApiKey)) return null;

            var decrypted = _encryptionService.Decrypt(company.GeminiApiKey);
            return !string.IsNullOrEmpty(decrypted) ? decrypted : null;
        }

        public async Task<string?> GetCompanyCommentPromptAsync(int companyId)
        {
            var company = await _dbContext.Companies.SingleOrDefaultAsync(c => c.CompanyId == companyId);
            return company?.GeminiPrompt;
        }

        public async Task<bool> IsAiModerationEnabledAsync(int companyId)
        {
            var company = await _dbContext.Companies.SingleOrDefaultAsync(c => c.CompanyId == companyId);
            return company?.IsAiModerationEnabled ?? false;
        }

        public async Task<bool> UpdateCompanyAiModerationAsync(int companyId, bool isEnabled)
        {
            var company = await _dbContext.Companies.SingleOrDefaultAsync(c => c.CompanyId == companyId);
            if (company == null)
            {
                return false;
            }
            company.IsAiModerationEnabled = isEnabled;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCompanyGeminiKeyAsync(int companyId, string apiKey)
        {
            var company = await _dbContext.Companies.SingleOrDefaultAsync(c => c.CompanyId == companyId);
            if (company == null) return false;

            company.GeminiApiKey = !string.IsNullOrEmpty(apiKey) ? _encryptionService.Encrypt(apiKey) : null;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCompanyGeminiSettingsAsync(int companyId, string apiKey, string? commentPrompt)
        {
            var company = await _dbContext.Companies.SingleOrDefaultAsync(c => c.CompanyId == companyId);
            if (company == null) return false;

            var existingKey = await GetCompanyGeminiKeyAsync(companyId) ?? string.Empty;
            var keyPart = apiKey;
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "*****")
            {
                keyPart = existingKey;
            }

            company.GeminiApiKey = !string.IsNullOrEmpty(keyPart) ? _encryptionService.Encrypt(keyPart) : null;
            company.GeminiPrompt = commentPrompt;
            
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<AiSettingsDto?> GetAiSettingsAsync(int companyId)
        {
            var company = await _dbContext.Companies.SingleOrDefaultAsync(c => c.CompanyId == companyId);
            if (company == null) return null;

            var decryptedKey = !string.IsNullOrEmpty(company.GeminiApiKey) 
                ? _encryptionService.Decrypt(company.GeminiApiKey) 
                : string.Empty;

            return new AiSettingsDto
            {
                GeminiApiKey = decryptedKey ?? string.Empty,
                CommentModerationPrompt = company.GeminiPrompt ?? string.Empty,
                IsAiModerationEnabled = company.IsAiModerationEnabled
            };
        }

        public async Task<bool> UpdateAiSettingsAsync(int companyId, AiSettingsDto settings)
        {
            var company = await _dbContext.Companies.SingleOrDefaultAsync(c => c.CompanyId == companyId);
            if (company == null) return false;

            if (!string.IsNullOrWhiteSpace(settings.GeminiApiKey) && settings.GeminiApiKey != "*****")
            {
                company.GeminiApiKey = _encryptionService.Encrypt(settings.GeminiApiKey);
            }

            company.GeminiPrompt = settings.CommentModerationPrompt;
            company.IsAiModerationEnabled = settings.IsAiModerationEnabled;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangePasswordAsync(long userId, string currentPassword, string newPassword)
        {
            var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.Password))
            {
                return false;
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangeEmailAsync(long userId, string newEmail, string password)
        {
            var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                return false;
            }

            // Check if email is already taken
            if (await _dbContext.Users.AnyAsync(u => u.Email == newEmail && u.Id != userId))
            {
                return false;
            }

            user.Email = newEmail;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateContactInfoAsync(long userId, string? phone, string? address)
        {
            var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            user.Phone = phone;
            user.Address = address;
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
