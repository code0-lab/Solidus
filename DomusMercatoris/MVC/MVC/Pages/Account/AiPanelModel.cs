using DomusMercatorisDotnetMVC.Services;

namespace DomusMercatorisDotnetMVC.Pages.Account
{
    public class AiPanelModel
    {
        public string GeminiApiKey { get; set; } = string.Empty;
        public bool IsAiModerationEnabled { get; set; }

        public void LoadSettings(UserService userService, int companyId)
        {
            GeminiApiKey = userService.GetCompanyGeminiKey(companyId) ?? string.Empty;
            IsAiModerationEnabled = userService.IsAiModerationEnabled(companyId);
        }

        public bool SaveSettings(UserService userService, int companyId)
        {
            var keyResult = userService.UpdateCompanyGeminiKey(companyId, GeminiApiKey);
            var modResult = userService.UpdateCompanyAiModeration(companyId, IsAiModerationEnabled);
            return keyResult && modResult;
        }
    }
}
