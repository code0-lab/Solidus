using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using DomusMercatoris.Service.Interfaces;

namespace DomusMercatoris.Service.Services
{
    public class ApiKeyService
    {
        private readonly DomusDbContext _db;
        private readonly ICurrentUserService _currentUserService;

        public ApiKeyService(DomusDbContext db, ICurrentUserService currentUserService)
        {
            _db = db;
            _currentUserService = currentUserService;
        }

        private void ValidateCompanyAccess(int companyId)
        {
            if (_currentUserService.CompanyId.HasValue && _currentUserService.CompanyId.Value != companyId)
            {
                throw new UnauthorizedAccessException("Cannot manage API keys for another company.");
            }
        }

        public async Task<(string PlainTextKey, ApiKey ApiKey)> CreateApiKeyAsync(int companyId, string name, int? expiryDays = null)
        {
            ValidateCompanyAccess(companyId);

            // 1. Generate a secure random key
            // Format: dm_sk_ + 32 chars (random bytes base64url or hex)
            var keyBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(keyBytes);
            }
            
            // Convert to a clean string (base62 or hex). Let's use Hex for simplicity and url safety.
            var keyString = Convert.ToHexString(keyBytes).ToLower();
            var plainTextKey = $"dm_sk_{keyString}";

            // 2. Hash the key
            var hash = ComputeHash(plainTextKey);

            // 3. Create Entity
            var apiKey = new ApiKey
            {
                CompanyId = companyId,
                Name = name,
                KeyHash = hash,
                Prefix = plainTextKey.Substring(0, 10), // "dm_sk_" + first 4 chars
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiryDays.HasValue ? DateTime.UtcNow.AddDays(expiryDays.Value) : null
            };

            _db.ApiKeys.Add(apiKey);
            await _db.SaveChangesAsync();

            return (plainTextKey, apiKey);
        }

        public async Task<int?> ValidateApiKeyAsync(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return null;

            // Optimistic check: Hash incoming key and look up
            var hash = ComputeHash(apiKey);

            var keyEntity = await _db.ApiKeys
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.KeyHash == hash);

            if (keyEntity == null) return null;
            if (!keyEntity.IsActive) return null;
            if (keyEntity.ExpiresAt.HasValue && keyEntity.ExpiresAt.Value < DateTime.UtcNow) return null;

            return keyEntity.CompanyId;
        }

        public async Task RevokeApiKeyAsync(int id, int companyId)
        {
            ValidateCompanyAccess(companyId);

            var key = await _db.ApiKeys.FirstOrDefaultAsync(k => k.Id == id && k.CompanyId == companyId);
            if (key != null)
            {
                key.IsActive = false;
                await _db.SaveChangesAsync();
            }
        }

        public async Task<(string PlainTextKey, ApiKey ApiKey)?> RegenerateApiKeyAsync(int id, int companyId)
        {
            ValidateCompanyAccess(companyId);

            var keyToUpdate = await _db.ApiKeys.FirstOrDefaultAsync(k => k.Id == id && k.CompanyId == companyId);
            if (keyToUpdate == null) return null;

            // 1. Generate a secure random key
            var keyBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(keyBytes);
            }
            
            var keyString = Convert.ToHexString(keyBytes).ToLower();
            var plainTextKey = $"dm_sk_{keyString}";

            // 2. Hash the key
            var hash = ComputeHash(plainTextKey);

            // 3. Update the existing entity (Overwrite)
            keyToUpdate.KeyHash = hash;
            keyToUpdate.Prefix = plainTextKey.Substring(0, 10);
            keyToUpdate.IsActive = true;
            keyToUpdate.CreatedAt = DateTime.UtcNow; // Update creation time to now
            
            // Note: We keep the original Name and ExpiresAt settings.
            
            await _db.SaveChangesAsync();

            return (plainTextKey, keyToUpdate);
        }
        
        public async Task DeleteApiKeyAsync(int id, int companyId)
        {
            ValidateCompanyAccess(companyId);

            var key = await _db.ApiKeys.FirstOrDefaultAsync(k => k.Id == id && k.CompanyId == companyId);
            if (key != null)
            {
                _db.ApiKeys.Remove(key);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<List<ApiKey>> GetApiKeysAsync(int companyId)
        {
            ValidateCompanyAccess(companyId);

            return await _db.ApiKeys
                .Where(k => k.CompanyId == companyId)
                .OrderByDescending(k => k.CreatedAt)
                .ToListAsync();
        }

        private static string ComputeHash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
