using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatorisDotnetMVC.Services;
using DomusMercatoris.Core.Entities;
using System.Collections.Generic;
using System.Linq;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Roles = "Manager")]
    public class CustomersModel : PageModel
    {
        private readonly UserService _userService;
        public CustomersModel(UserService userService)
        {
            _userService = userService;
        }

        public List<User> Customers { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var comp = User.FindFirst("CompanyId")?.Value;
            if (!string.IsNullOrEmpty(comp) && int.TryParse(comp, out var companyId))
            {
                var customers = await _userService.GetByCompanyAsync(companyId);
                Customers = customers
                    .Where(u => (u.Roles ?? new List<string>()).Any(r => string.Equals(r, "Customer", System.StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                return Page();
            }
            var idClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(idClaim) || !long.TryParse(idClaim, out var userId))
            {
                return RedirectToPage("/Index");
            }
            var me = await _userService.GetByIdAsync(userId);
            if (me == null)
            {
                return RedirectToPage("/Index");
            }
            var customersMe = await _userService.GetByCompanyAsync(me.CompanyId);
            Customers = customersMe
                .Where(u => (u.Roles ?? new List<string>()).Any(r => string.Equals(r, "Customer", System.StringComparison.OrdinalIgnoreCase)))
                .ToList();
            return Page();
        }
    }
}
