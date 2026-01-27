using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatorisDotnetMVC.Services;
using DomusMercatoris.Core.Entities;
using DomusMercatorisDotnetMVC.Dto.UserDto;
using DomusMercatoris.Service.DTOs;

namespace DomusMercatorisDotnetMVC.Pages.Account
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly UserService _userService;
        private readonly GeminiService _geminiService;

        public ProfileModel(UserService userService, GeminiService geminiService)
        {
            _userService = userService;
            _geminiService = geminiService;
        }

        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();

        [BindProperty]
        public AiPanelModel AiPanel { get; set; } = new();

        [BindProperty]
        public UpdateUserProfileDto ContactInfo { get; set; } = new();

        [BindProperty]
        public ChangePasswordDto PasswordChange { get; set; } = new();

        [BindProperty]
        public ChangeEmailDto EmailChange { get; set; } = new();

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

        public async Task<IActionResult> OnPostUpdateAiSettingsAsync()
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

                bool success = await _geminiService.UpdateAiSettingsAsync(user.CompanyId, aiSettingsDto);

                if (success)
                {
                    TempData["SuccessMessage"] = "AI settings updated successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update AI settings.";
                }

                await LoadPageDataAsync(user);
                return Page();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostUpdateContactAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToPage("/Index");

            var success = await _userService.UpdateContactInfoAsync(user.Id, ContactInfo.Phone, ContactInfo.Address);
            if (success)
            {
                TempData["SuccessMessage"] = "Contact info updated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update contact info.";
            }

            await LoadPageDataAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToPage("/Index");

            if (!ModelState.IsValid)
            {
                 // Filter relevant validation errors
                 foreach (var state in ModelState)
                 {
                     if (state.Key.StartsWith("PasswordChange") && state.Value.Errors.Count > 0)
                     {
                         TempData["ErrorMessage"] = state.Value.Errors.First().ErrorMessage;
                         await LoadPageDataAsync(user);
                         return Page();
                     }
                 }
            }

            var success = await _userService.ChangePasswordAsync(user.Id, PasswordChange.CurrentPassword, PasswordChange.NewPassword);
            if (success)
            {
                TempData["SuccessMessage"] = "Password changed successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to change password. Check your current password.";
            }

            await LoadPageDataAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToPage("/Index");

             if (!ModelState.IsValid)
            {
                 // Filter relevant validation errors
                 foreach (var state in ModelState)
                 {
                     if (state.Key.StartsWith("EmailChange") && state.Value.Errors.Count > 0)
                     {
                         TempData["ErrorMessage"] = state.Value.Errors.First().ErrorMessage;
                         await LoadPageDataAsync(user);
                         return Page();
                     }
                 }
            }

            var success = await _userService.ChangeEmailAsync(user.Id, EmailChange.NewEmail, EmailChange.CurrentPassword);
            if (success)
            {
                TempData["SuccessMessage"] = "Email updated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update email. Password might be wrong or email already taken.";
            }

            await LoadPageDataAsync(user);
            return Page();
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
            
            ContactInfo.Phone = user.Phone;
            ContactInfo.Address = user.Address;
            EmailChange.NewEmail = user.Email;

            // Load AI settings using service directly (Only for Managers)
            if (Roles.Any(r => r.Trim().Equals("Manager", StringComparison.OrdinalIgnoreCase)))
            {
                var aiSettings = await _geminiService.GetAiSettingsAsync(user.CompanyId);
                if (aiSettings != null)
                {
                    AiPanel.ExistingGeminiApiKey = aiSettings.GeminiApiKey;
                    AiPanel.GeminiApiKey = string.IsNullOrEmpty(aiSettings.GeminiApiKey) ? string.Empty : "*****";
                    AiPanel.IsAiModerationEnabled = aiSettings.IsAiModerationEnabled;
                    AiPanel.CommentModerationPrompt = aiSettings.CommentModerationPrompt;
                }
            }
        }
    }
}
