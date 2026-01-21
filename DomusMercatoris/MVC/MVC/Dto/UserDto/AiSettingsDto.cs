namespace DomusMercatorisDotnetMVC.Dto.UserDto
{
    public class AiSettingsDto
    {
        public string GeminiApiKey { get; set; } = string.Empty;
        public string CommentModerationPrompt { get; set; } = string.Empty;
        public bool IsAiModerationEnabled { get; set; }
    }
}