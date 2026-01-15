using DomusMercatoris.Data;
using System.Linq;
using System;

namespace DomusMercatoris.Service.Services
{
    public class CompanySettingsService
    {
        private readonly DomusDbContext _db;
        private readonly EncryptionService _encryptionService;
        private const string GeminiCommentSeparator = "\n---COMMENT_PROMPT---\n";

        public CompanySettingsService(DomusDbContext db, EncryptionService encryptionService)
        {
            _db = db;
            _encryptionService = encryptionService;
        }

        public bool IsAiModerationEnabled(int companyId)
        {
            var company = _db.Companies.SingleOrDefault(c => c.CompanyId == companyId);
            return company?.IsAiModerationEnabled ?? false;
        }

        public string? GetCompanyGeminiKey(int companyId)
        {
            var company = _db.Companies.SingleOrDefault(c => c.CompanyId == companyId);
            if (string.IsNullOrEmpty(company?.GeminiApiKey)) return null;

            var decrypted = _encryptionService.Decrypt(company.GeminiApiKey);
            if (string.IsNullOrEmpty(decrypted)) return null;

            var parts = decrypted.Split(GeminiCommentSeparator, 2, StringSplitOptions.None);
            return parts.Length > 0 ? parts[0] : null;
        }

        public string? GetCompanyCommentPrompt(int companyId)
        {
            var company = _db.Companies.SingleOrDefault(c => c.CompanyId == companyId);
            if (string.IsNullOrEmpty(company?.GeminiApiKey)) return null;

            var decrypted = _encryptionService.Decrypt(company.GeminiApiKey);
            if (string.IsNullOrEmpty(decrypted)) return null;

            var parts = decrypted.Split(GeminiCommentSeparator, 2, StringSplitOptions.None);
            return parts.Length > 1 ? parts[1] : null;
        }
    }
}
