using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatorisDotnetMVC.Services;
using DomusMercatoris.Core.Entities;
using DomusMercatorisDotnetMVC.Dto.UserDto;

namespace DomusMercatorisDotnetMVC.Pages.Account
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly UserService _userService;
        public ProfileModel(UserService userService)
        {
            _userService = userService;
        }

        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();

        [BindProperty]
        public AiPanelModel AiPanel { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToPage("/Index");
            }

            await LoadPageDataAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user == null)
                {
                    return RedirectToPage("/Index");
                }

                // Security check: Only Managers can update AI settings
                if (user.Roles == null || !user.Roles.Any(r => r.Trim().Equals("Manager", StringComparison.OrdinalIgnoreCase)))
                {
                    return Forbid();
                }

                // Save settings using service directly
                var aiSettingsDto = new AiSettingsDto
                {
                    GeminiApiKey = AiPanel.GeminiApiKey,
                    CommentModerationPrompt = AiPanel.CommentModerationPrompt,
                    IsAiModerationEnabled = AiPanel.IsAiModerationEnabled
                };

                bool success = await _userService.UpdateAiSettingsAsync(user.CompanyId, aiSettingsDto);

                if (success)
                {
                    TempData["SuccessMessage"] = "Profile settings updated successfully.";
                    return RedirectToPage();
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update some settings.";
                }

                // Re-populate view data
                await LoadPageDataAsync(user);
                
                return Page();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToPage();
            }
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            var idClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(idClaim) || !long.TryParse(idClaim, out var userId))
            {
                return null;
            }
            return await _userService.GetByIdAsync(userId);
        }

        private async Task LoadPageDataAsync(User user)
        {
            FullName = $"{user.FirstName} {user.LastName}";
            Email = user.Email;
            CompanyId = user.CompanyId;
            CompanyName = await _userService.GetCompanyNameAsync(user.CompanyId) ?? string.Empty;
            Roles = user.Roles ?? new List<string>();
            
            // Load AI settings using service directly (Only for Managers)
            if (Roles.Any(r => r.Trim().Equals("Manager", StringComparison.OrdinalIgnoreCase)))
            {
                var aiSettings = await _userService.GetAiSettingsAsync(user.CompanyId);
                if (aiSettings != null)
                {
                    // Only load the existing API key if it belongs to the user's company
                    // If the user is a Manager but there is no API key yet, they can enter one.
                    AiPanel.ExistingGeminiApiKey = aiSettings.GeminiApiKey;
                    AiPanel.GeminiApiKey = string.IsNullOrEmpty(aiSettings.GeminiApiKey) ? string.Empty : "*****";
                    AiPanel.IsAiModerationEnabled = aiSettings.IsAiModerationEnabled;
                    AiPanel.CommentModerationPrompt = aiSettings.CommentModerationPrompt;
                }
            }
        }
    }
}
