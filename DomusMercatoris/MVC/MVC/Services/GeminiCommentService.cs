using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DomusMercatorisDotnetMVC.Services
{
    public class GeminiCommentService
    {
        private readonly HttpClient _httpClient;
        private readonly UserService _userService;

        public GeminiCommentService(HttpClient httpClient, UserService userService)
        {
            _httpClient = httpClient;
            _userService = userService;
        }

        public async Task<bool> ModerateCommentAsync(string commentText, int companyId)
        {
            if (!_userService.IsAiModerationEnabled(companyId))
            {
                return true;
            }

            var apiKey = _userService.GetCompanyGeminiKey(companyId);
            if (string.IsNullOrEmpty(apiKey))
            {
                return true;
            }

            var prompt = $"Analyze the following comment for profanity or swearing. " +
                         $"Respond with ONLY 'true' if the comment DOES NOT contain profanity (it is safe), or 'false' if it contains profanity. " +
                         $"Do not provide any explanation, just the boolean value. Comment: \"{commentText}\"";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={apiKey}";

            try
            {
                var response = await _httpClient.PostAsync(url, jsonContent);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                
                // Navigate the JSON response structure safely
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
                
                // Fallback if response format is unexpected
                return true; 
            }
            catch (Exception)
            {
                // In case of API error, default to approval (or could log and flag for manual review)
                return true;
            }
        }
    }
}
