using DomusMercatoris.Data;
using System.Linq;
using System;
using Microsoft.EntityFrameworkCore;

namespace DomusMercatoris.Service.Services
{
    public class CompanySettingsService
    {
        private readonly DomusDbContext _db;
        private readonly EncryptionService _encryptionService;

        public CompanySettingsService(DomusDbContext db, EncryptionService encryptionService)
        {
            _db = db;
            _encryptionService = encryptionService;
        }

        public bool IsAiModerationEnabled(int companyId)
        {
            var company = _db.Companies.AsNoTracking().SingleOrDefault(c => c.CompanyId == companyId);
            return company?.IsAiModerationEnabled ?? false;
        }

        public string? GetCompanyGeminiKey(int companyId)
        {
            var company = _db.Companies.AsNoTracking().SingleOrDefault(c => c.CompanyId == companyId);
            if (company == null) return null;
            if (string.IsNullOrEmpty(company.GeminiApiKey)) return null;

            var decrypted = _encryptionService.Decrypt(company.GeminiApiKey);
            return !string.IsNullOrEmpty(decrypted) ? decrypted : null;
        }

        public string? GetCompanyCommentPrompt(int companyId)
        {
            var company = _db.Companies.AsNoTracking().SingleOrDefault(c => c.CompanyId == companyId);
            return company?.GeminiPrompt;
        }
    }
}
