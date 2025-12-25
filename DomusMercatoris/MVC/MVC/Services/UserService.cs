using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using DomusMercatorisDotnetMVC.Dto.UserDto;
using DomusMercatorisDotnetMVC.Models;
using DomusMercatorisDotnetMVC.Utils;

namespace DomusMercatorisDotnetMVC.Services
{
    public class UserService
    {

        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly EncryptionService _encryptionService;

        public UserService(ApplicationDbContext dbContext, IMapper mapper, EncryptionService encryptionService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _encryptionService = encryptionService;
        }

        public User? UserLogin(string Email, string Password)
        {
            var user =  _dbContext.Users.Include(u => u.Ban).SingleOrDefault(u => u.Email == Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(Password, user.Password))
            {
                return null;
            }
            return user;
        }

        public User UserRegister(UserRegisterDto userRegisterDto)
        {
            var userEntity = _mapper.Map<User>(userRegisterDto);
            userEntity.Password = BCrypt.Net.BCrypt.HashPassword(userRegisterDto.Password);
            userEntity.Roles = new List<string> { "Manager", "User", "Customer" };

            var companyName = (userRegisterDto.CompanyName ?? string.Empty).Trim();
            var company = new Company { Name = companyName };
            _dbContext.Companies.Add(company);
            _dbContext.SaveChanges();

            userEntity.CompanyId = company.CompanyId;
            _dbContext.Users.Add(userEntity);
            _dbContext.SaveChanges();
            return userEntity;
        }

        public User RegisterWorker(UserRegisterDto userRegisterDto, int companyId)
        {
            var userEntity = _mapper.Map<User>(userRegisterDto);
            userEntity.Password = BCrypt.Net.BCrypt.HashPassword(userRegisterDto.Password);
            userEntity.Roles = new List<string> { "User" };
            userEntity.CompanyId = companyId;
            _dbContext.Users.Add(userEntity);
            _dbContext.SaveChanges();
            return userEntity;
        }

        public User? GetById(long id)
        {
            return _dbContext.Users.SingleOrDefault(u => u.Id == id);
        }

        public List<User> GetByCompany(int companyId)
        {
            return _dbContext.Users.Where(u => u.CompanyId == companyId).OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToList();
        }

        public List<User> SearchByCompany(int companyId, string query, int limit = 20)
        {
            var q = (query ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(q)) return new List<User>();
            q = q.ToLowerInvariant();
            return _dbContext.Users
                .Where(u => u.CompanyId == companyId && (
                    (u.FirstName ?? string.Empty).ToLower().Contains(q) ||
                    (u.LastName ?? string.Empty).ToLower().Contains(q) ||
                    (u.Email ?? string.Empty).ToLower().Contains(q)
                ))
                .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
                .Take(limit)
                .ToList();
        }

        public bool UpdateUserInCompany(long id, int companyId, string firstName, string lastName, string email)
        {
            var user = _dbContext.Users.SingleOrDefault(u => u.Id == id && u.CompanyId == companyId);
            if (user == null)
            {
                return false;
            }
            var existsEmail = _dbContext.Users.Any(u => u.Email == email && u.Id != id);
            if (existsEmail)
            {
                return false;
            }
            user.FirstName = firstName;
            user.LastName = lastName;
            user.Email = email;
            _dbContext.SaveChanges();
            return true;
        }

        public bool DeleteUserInCompany(long id, int companyId)
        {
            var user = _dbContext.Users.SingleOrDefault(u => u.Id == id && u.CompanyId == companyId);
            if (user == null)
            {
                return false;
            }
            _dbContext.Users.Remove(user);
            _dbContext.SaveChanges();
            return true;
        }

        public string? GetCompanyName(int companyId)
        {
            var company = _dbContext.Companies.SingleOrDefault(c => c.CompanyId == companyId);
            return company?.Name;
        }

        public string? GetCompanyGeminiKey(int companyId)
        {
            var company = _dbContext.Companies.SingleOrDefault(c => c.CompanyId == companyId);
            return string.IsNullOrEmpty(company?.GeminiApiKey) ? null : _encryptionService.Decrypt(company.GeminiApiKey);
        }

        public bool IsAiModerationEnabled(int companyId)
        {
            var company = _dbContext.Companies.SingleOrDefault(c => c.CompanyId == companyId);
            return company?.IsAiModerationEnabled ?? false;
        }

        public bool UpdateCompanyAiModeration(int companyId, bool isEnabled)
        {
            var company = _dbContext.Companies.SingleOrDefault(c => c.CompanyId == companyId);
            if (company == null)
            {
                return false;
            }
            company.IsAiModerationEnabled = isEnabled;
            _dbContext.SaveChanges();
            return true;
        }

        public bool UpdateCompanyGeminiKey(int companyId, string apiKey)
        {
            var company = _dbContext.Companies.SingleOrDefault(c => c.CompanyId == companyId);
            if (company == null) return false;
            company.GeminiApiKey = _encryptionService.Encrypt(apiKey);
            _dbContext.SaveChanges();
            return true;
        }
    }
}
