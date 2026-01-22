using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize]
    public class BannedModel : PageModel
    {
        private readonly DomusDbContext _context;

        public BannedModel(DomusDbContext context)
        {
            _context = context;
        }

        public Ban BanInfo { get; set; } = default!;
        public string RemainingTime { get; set; } = string.Empty;
        public string? Message { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr))
            {
                 return RedirectToPage("/Index");
            }

            var userId = long.Parse(userIdStr);
            var user = await _context.Users.Include(u => u.Ban).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.Ban == null || !user.Ban.IsBanned)
            {
                 if (User.IsInRole("Banned"))
                 {
                     await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                     return RedirectToPage("/Index");
                 }
                return RedirectToPage("/Dashboard");
            }

            BanInfo = user.Ban;
            
            if (!BanInfo.PermaBan && BanInfo.EndDate.HasValue)
            {
                var remaining = BanInfo.EndDate.Value - DateTime.UtcNow;
                if (remaining.TotalDays >= 1)
                    RemainingTime = $"{(int)remaining.TotalDays} days, {remaining.Hours} hours";
                else
                    RemainingTime = $"{remaining.Hours} hours, {remaining.Minutes} minutes";
            }
            else
            {
                RemainingTime = "Permanent";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string objection)
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
             if (string.IsNullOrEmpty(userIdStr))
            {
                 return RedirectToPage("/Index");
            }
            var userId = long.Parse(userIdStr);
            var user = await _context.Users.Include(u => u.Ban).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.Ban == null)
            {
                return RedirectToPage("/Index");
            }

            BanInfo = user.Ban;

            if (!string.IsNullOrWhiteSpace(objection))
            {
                BanInfo.Object = objection;
                BanInfo.ObjectToBan = true;
                await _context.SaveChangesAsync();
                Message = "Your appeal has been submitted successfully.";
            }

            return RedirectToPage();
        }
    }
}
