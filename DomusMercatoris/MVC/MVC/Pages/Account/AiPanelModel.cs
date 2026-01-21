using DomusMercatorisDotnetMVC.Services;
using DomusMercatorisDotnetMVC.Dto.UserDto;

namespace DomusMercatorisDotnetMVC.Pages.Account
{
    public class AiPanelModel
    {
        public string GeminiApiKey { get; set; } = string.Empty;
        public bool IsAiModerationEnabled { get; set; }
        public string CommentModerationPrompt { get; set; } = string.Empty;
        public string ExistingGeminiApiKey { get; set; } = string.Empty;
    }
}