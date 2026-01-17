using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;
using System;

namespace DomusMercatoris.Service.Services
{
    public interface IGeminiCommentService
    {
        Task<bool> ModerateCommentAsync(string commentText, int companyId);
        Task<string?> EvaluateCommentWithPromptAsync(string commentText, int companyId, string prompt);
    }

    public class GeminiCommentService : IGeminiCommentService
    {
        private readonly HttpClient _httpClient;
        private readonly CompanySettingsService _companySettingsService;

        public GeminiCommentService(HttpClient httpClient, CompanySettingsService companySettingsService)
        {
            _httpClient = httpClient;
            _companySettingsService = companySettingsService;
        }

        public async Task<bool> ModerateCommentAsync(string commentText, int companyId)
        {
            if (!_companySettingsService.IsAiModerationEnabled(companyId))
            {
                // Default to true (safe) if moderation is disabled
                return true;
            }

            var apiKey = _companySettingsService.GetCompanyGeminiKey(companyId);
            if (string.IsNullOrEmpty(apiKey))
            {
                return true;
            }

            var customPrompt = _companySettingsService.GetCompanyCommentPrompt(companyId);
            string prompt;
            if (!string.IsNullOrWhiteSpace(customPrompt))
            {
                prompt = $"{customPrompt}\n\nCevabın sadece 'true' veya 'false' olsun.\nYorum: \"{commentText}\"";
            }
            else
            {
                prompt = $"Analyze the following comment for profanity or swearing. " +
                         $"Respond with ONLY 'true' if the comment DOES NOT contain profanity (it is safe), or 'false' if it contains profanity. " +
                         $"Do not provide any explanation, just the boolean value. Comment: \"{commentText}\"";
            }

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent?key={apiKey}";

            try
            {
                var response = await _httpClient.PostAsync(url, jsonContent);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                
                var root = doc.RootElement;
                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var content = candidates[0].GetProperty("content");
                    var parts = content.GetProperty("parts");
                    if (parts.GetArrayLength() > 0)
                    {
                        var text = parts[0].GetProperty("text").GetString()?.Trim().ToLower();
                        return text == "true";
                    }
                }
                
                return true; 
            }
            catch (Exception)
            {
                return true;
            }
        }

        public async Task<string?> EvaluateCommentWithPromptAsync(string commentText, int companyId, string prompt)
        {
            if (!_companySettingsService.IsAiModerationEnabled(companyId))
            {
                return null;
            }

            var apiKey = _companySettingsService.GetCompanyGeminiKey(companyId);
            if (string.IsNullOrEmpty(apiKey))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                return null;
            }

            var fullPrompt = $"{prompt}\n\nComment: \"{commentText}\"";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = fullPrompt } } }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent?key={apiKey}";

            try
            {
                var response = await _httpClient.PostAsync(url, jsonContent);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);

                var root = doc.RootElement;
                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var content = candidates[0].GetProperty("content");
                    var parts = content.GetProperty("parts");
                    if (parts.GetArrayLength() > 0)
                    {
                        return parts[0].GetProperty("text").GetString();
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
