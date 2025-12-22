using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatorisDotnetMVC.Services;

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

        public void OnGet()
        {
            var idClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(idClaim) || !long.TryParse(idClaim, out var userId))
            {
                Response.Redirect("/Index");
                return;
            }
            var user = _userService.GetById(userId);
            if (user == null)
            {
                Response.Redirect("/Index");
                return;
            }
            FullName = $"{user.FirstName} {user.LastName}";
            Email = user.Email;
            CompanyId = user.CompanyId;
            CompanyName = _userService.GetCompanyName(user.CompanyId) ?? string.Empty;
            Roles = user.Roles ?? new List<string>();
            
            // Load AI settings using the helper model
            AiPanel.LoadSettings(_userService, user.CompanyId);
        }

        public IActionResult OnPost()
        {
            try
            {
                var idClaim = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(idClaim) || !long.TryParse(idClaim, out var userId))
                {
                    return RedirectToPage("/Index");
                }
                var user = _userService.GetById(userId);
                if (user == null)
                {
                    return RedirectToPage("/Index");
                }

                // Security check: Only Managers can update AI settings
                if (user.Roles == null || !user.Roles.Contains("Manager"))
                {
                    return Forbid();
                }

                // Checkbox handling for AiPanel.IsAiModerationEnabled
                // If the checkbox is unchecked, it won't be sent in the form data.
                // However, since we use [BindProperty] on AiPanel, the model binder should handle it.
                // But just to be safe with partial view binding prefixes:
                if (!Request.Form.ContainsKey("AiPanel.IsAiModerationEnabled"))
                {
                    AiPanel.IsAiModerationEnabled = false;
                }

                // Save settings using the helper model
                bool success = AiPanel.SaveSettings(_userService, user.CompanyId);

                if (success)
                {
                    TempData["SuccessMessage"] = "Profile settings updated successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update some settings.";
                }

                // Re-populate view data
                FullName = $"{user.FirstName} {user.LastName}";
                Email = user.Email;
                CompanyId = user.CompanyId;
                CompanyName = _userService.GetCompanyName(user.CompanyId) ?? string.Empty;
                Roles = user.Roles ?? new List<string>();
                
                // Reload to reflect saved state
                AiPanel.LoadSettings(_userService, user.CompanyId);
                
                return Page();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToPage();
            }
        }
    }
}
