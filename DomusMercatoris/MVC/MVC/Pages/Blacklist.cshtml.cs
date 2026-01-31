using DomusMercatoris.Service.Services;
using DomusMercatorisDotnetMVC.Services;
using DomusMercatoris.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize]
    public class BlacklistModel : PageModel
    {
        private readonly BlacklistService _blacklistService;
        private readonly UserService _userService;
        private readonly DomusMercatoris.Data.DomusDbContext _context; // Direct access for company names

        public BlacklistModel(BlacklistService blacklistService, UserService userService, DomusMercatoris.Data.DomusDbContext context)
        {
            _blacklistService = blacklistService;
            _userService = userService;
            _context = context;
        }

        public class BlockedCompanyDto
        {
            public int CompanyId { get; set; }
            public string CompanyName { get; set; } = string.Empty;
        }

        public List<BlockedCompanyDto> MyBlockedCompanies { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdStr, out var userId))
            {
                var blockedIds = await _blacklistService.GetCompaniesBlockedByCustomerAsync(userId);
                
                // Fetch company names
                MyBlockedCompanies = _context.Companies
                    .Where(c => blockedIds.Contains(c.CompanyId))
                    .Select(c => new BlockedCompanyDto
                    {
                        CompanyId = c.CompanyId,
                        CompanyName = c.Name
                    })
                    .ToList();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostUnblockAsync(int companyId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdStr, out var userId))
            {
                await _blacklistService.UnblockByCustomerAsync(userId, companyId);
            }
            return RedirectToPage();
        }
    }
}
