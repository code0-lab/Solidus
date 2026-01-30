using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
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

        public async Task<int> GetPendingTaskCountAsync(long userId)
        {
            return await _dbContext.WorkTasks
                .CountAsync(t => t.AssignedToUserId == userId && !t.IsCompleted);
        }

        public async Task<int> GetCompanyIdFromUserAsync(ClaimsPrincipal user)
        {
            var compClaim = user.FindFirst("CompanyId")?.Value;
            if (!string.IsNullOrEmpty(compClaim) && int.TryParse(compClaim, out var cid))
            {
                return cid;
            }
            
            var idClaim = user.FindFirst("UserId")?.Value;
            if (!string.IsNullOrEmpty(idClaim) && long.TryParse(idClaim, out var userId))
            {
                var userEntity = await _dbContext.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId);
                if (userEntity != null) return userEntity.CompanyId;
            }
            return 0;
        }

        public async Task<List<User>> SearchUsersAsync(string? search)
        {
            var query = _dbContext.Users.Include(u => u.Ban).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower().Trim();
                query = query.Where(u => 
                    u.Email.ToLower().Contains(s) || 
                    u.FirstName.ToLower().Contains(s) || 
                    u.LastName.ToLower().Contains(s));
            }

            return await query.OrderBy(u => u.Email).Take(50).ToListAsync();
        }

        public async Task<(List<User> Users, int TotalCount)> SearchUsersPagedAsync(string? search, int pageIndex, int pageSize)
        {
            var query = _dbContext.Users.Include(u => u.Ban).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower().Trim();
                query = query.Where(u => 
                    u.Email.ToLower().Contains(s) || 
                    u.FirstName.ToLower().Contains(s) || 
                    u.LastName.ToLower().Contains(s));
            }

            var totalCount = await query.CountAsync();
            var users = await query.OrderBy(u => u.Email)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (users, totalCount);
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

        public async Task<bool> DeleteUserInCompanyAsync(long id, int companyId, long currentManagerId)
        {
            var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId);
            if (user == null)
            {
                return false;
            }

            // Security Check: Prevent deletion of Rex or Moderator
            if ((user.Roles ?? new List<string>()).Any(r => r == "Rex" || r == "Moderator") || string.Equals(user.Email, "rex@domus.com", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // 1. Delete tasks assigned to this user
            var assignedTasks = await _dbContext.WorkTasks.Where(t => t.AssignedToUserId == id).ToListAsync();
            if (assignedTasks.Any())
            {
                _dbContext.WorkTasks.RemoveRange(assignedTasks);
            }

            // 2. Reassign tasks created by this user to the current manager (to avoid Restrict constraint)
            var createdTasks = await _dbContext.WorkTasks.Where(t => t.CreatedByUserId == id).ToListAsync();
            foreach (var task in createdTasks)
            {
                task.CreatedByUserId = currentManagerId;
            }

            // Remove UserPageAccess records first (Cascade delete manually due to Restrict behavior)
            var accesses = await _dbContext.UserPageAccesses.Where(a => a.UserId == id).ToListAsync();
            if (accesses.Any())
            {
                _dbContext.UserPageAccesses.RemoveRange(accesses);
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



        public async Task<List<UserPageAccess>> GetUserPageAccessesForCompanyAsync(int companyId)
        {
            return await _dbContext.UserPageAccesses
                .AsNoTracking()
                .Where(a => a.CompanyId == companyId)
                .ToListAsync();
        }

        public async Task UpdateUserPageAccessAsync(int companyId, long userId, List<string> pageKeys)
        {
            var keys = pageKeys ?? new List<string>();
            var normalized = new HashSet<string>(keys.Where(k => !string.IsNullOrWhiteSpace(k)), StringComparer.OrdinalIgnoreCase);

            var existing = await _dbContext.UserPageAccesses
                .Where(a => a.CompanyId == companyId && a.UserId == userId)
                .ToListAsync();

            foreach (var access in existing)
            {
                if (!normalized.Contains(access.PageKey))
                {
                    _dbContext.UserPageAccesses.Remove(access);
                }
            }

            var existingKeys = new HashSet<string>(existing.Select(a => a.PageKey), StringComparer.OrdinalIgnoreCase);
            foreach (var key in normalized)
            {
                if (!existingKeys.Contains(key))
                {
                    _dbContext.UserPageAccesses.Add(new UserPageAccess
                    {
                        UserId = userId,
                        CompanyId = companyId,
                        PageKey = key
                    });
                }
            }

            await _dbContext.SaveChangesAsync();
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
