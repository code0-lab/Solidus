using DomusMercatoris.Data;
using System.Linq;
using System;
using Microsoft.EntityFrameworkCore;
using DomusMercatoris.Service.Interfaces;

namespace DomusMercatoris.Service.Services
{
    public class CompanySettingsService
    {
        private readonly DomusDbContext _db;
        private readonly EncryptionService _encryptionService;
        private readonly ICurrentUserService _currentUserService;

        public CompanySettingsService(DomusDbContext db, EncryptionService encryptionService, ICurrentUserService currentUserService)
        {
            _db = db;
            _encryptionService = encryptionService;
            _currentUserService = currentUserService;
        }

        private void ValidateCompanyAccess(int companyId)
        {
            if (_currentUserService.CompanyId.HasValue && _currentUserService.CompanyId.Value != companyId)
            {
                throw new UnauthorizedAccessException("Cannot access settings for another company.");
            }
        }

        public bool IsAiModerationEnabled(int companyId)
        {
            ValidateCompanyAccess(companyId);
            var company = _db.Companies.AsNoTracking().SingleOrDefault(c => c.CompanyId == companyId);
            return company?.IsAiModerationEnabled ?? false;
        }

        public string? GetCompanyGeminiKey(int companyId)
        {
            ValidateCompanyAccess(companyId);
            var company = _db.Companies.AsNoTracking().SingleOrDefault(c => c.CompanyId == companyId);
            if (company == null) return null;
            if (string.IsNullOrEmpty(company.GeminiApiKey)) return null;

            var decrypted = _encryptionService.Decrypt(company.GeminiApiKey);
            return !string.IsNullOrEmpty(decrypted) ? decrypted : null;
        }

        public string? GetCompanyCommentPrompt(int companyId)
        {
            ValidateCompanyAccess(companyId);
            var company = _db.Companies.AsNoTracking().SingleOrDefault(c => c.CompanyId == companyId);
            return company?.GeminiPrompt;
        }
    }
}
