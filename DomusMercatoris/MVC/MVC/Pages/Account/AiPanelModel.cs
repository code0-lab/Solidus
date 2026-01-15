using DomusMercatorisDotnetMVC.Services;

namespace DomusMercatorisDotnetMVC.Pages.Account
{
    public class AiPanelModel
    {
        public string GeminiApiKey { get; set; } = string.Empty;
        public bool IsAiModerationEnabled { get; set; }
        public string CommentModerationPrompt { get; set; } = string.Empty;
        public string ExistingGeminiApiKey { get; set; } = string.Empty;

        public void LoadSettings(UserService userService, int companyId)
        {
            var realKey = userService.GetCompanyGeminiKey(companyId) ?? string.Empty;
            ExistingGeminiApiKey = realKey;
            GeminiApiKey = string.IsNullOrEmpty(realKey) ? string.Empty : "*****";
            IsAiModerationEnabled = userService.IsAiModerationEnabled(companyId);
            CommentModerationPrompt = userService.GetCompanyCommentPrompt(companyId) ?? string.Empty;
        }

        public bool SaveSettings(UserService userService, int companyId)
        {
            var keyResult = userService.UpdateCompanyGeminiSettings(companyId, GeminiApiKey, CommentModerationPrompt);
            var modResult = userService.UpdateCompanyAiModeration(companyId, IsAiModerationEnabled);
            return keyResult && modResult;
        }
    }
}
