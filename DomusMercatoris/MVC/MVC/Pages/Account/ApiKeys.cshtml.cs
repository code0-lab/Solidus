using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Service.Services;
using DomusMercatorisDotnetMVC.Services;
using DomusMercatoris.Core.Constants;

namespace DomusMercatorisDotnetMVC.Pages.Account
{
    [Authorize]
    public class ApiKeysModel : PageModel
    {
        private readonly ApiKeyService _apiKeyService;
        private readonly UserService _userService;

        public ApiKeysModel(ApiKeyService apiKeyService, UserService userService)
        {
            _apiKeyService = apiKeyService;
            _userService = userService;
        }

        public List<ApiKey> ApiKeys { get; set; } = new();

        [BindProperty]
        public string NewKeyName { get; set; } = string.Empty;

        [TempData]
        public string? CreatedApiKey { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var companyId = await GetCompanyIdAsync();
            if (companyId == null) return RedirectToPage("/Index");

            ApiKeys = await _apiKeyService.GetApiKeysAsync(companyId.Value);
            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            var companyId = await GetCompanyIdAsync();
            if (companyId == null) return RedirectToPage("/Index");

            if (string.IsNullOrWhiteSpace(NewKeyName))
            {
                ModelState.AddModelError("NewKeyName", "Name is required.");
                ApiKeys = await _apiKeyService.GetApiKeysAsync(companyId.Value);
                return Page();
            }

            var result = await _apiKeyService.CreateApiKeyAsync(companyId.Value, NewKeyName);
            CreatedApiKey = result.PlainTextKey;
            
            TempData["SuccessMessage"] = "API Key created successfully. Copy it now, you won't see it again!";
            
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRevokeAsync(int id)
        {
            var companyId = await GetCompanyIdAsync();
            if (companyId == null) return RedirectToPage("/Index");

            await _apiKeyService.RevokeApiKeyAsync(id, companyId.Value);
            TempData["SuccessMessage"] = "API Key revoked.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRegenerateAsync(int id)
        {
            var companyId = await GetCompanyIdAsync();
            if (companyId == null) return RedirectToPage("/Index");

            var result = await _apiKeyService.RegenerateApiKeyAsync(id, companyId.Value);
            if (result == null)
            {
                return NotFound();
            }

            CreatedApiKey = result.Value.PlainTextKey;
            TempData["SuccessMessage"] = "API Key regenerated successfully. Copy the new key now!";
            
            return RedirectToPage();
        }
        
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var companyId = await GetCompanyIdAsync();
            if (companyId == null) return RedirectToPage("/Index");

            await _apiKeyService.DeleteApiKeyAsync(id, companyId.Value);
            TempData["SuccessMessage"] = "API Key deleted.";
            return RedirectToPage();
        }

        private async Task<int?> GetCompanyIdAsync()
        {
             var user = await _userService.GetByIdAsync(long.Parse(User.FindFirst("UserId")?.Value ?? "0"));
             if (user == null || !user.CompanyId.HasValue) return null;
             
             // Optional: Check if user is Manager or has permission
             if (user.Roles == null || !user.Roles.Any(r => r.Trim().Equals(AppConstants.Roles.Manager, StringComparison.OrdinalIgnoreCase)))
             {
                 return null;
             }

             return user.CompanyId;
        }
    }
}
