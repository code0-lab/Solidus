using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;

namespace DomusMercatorisDotnetMVC.Pages.Moderator
{
    [Authorize(Roles = "Moderator,Rex")]
    public class BanModel : PageModel
    {
        private readonly DomusDbContext _context;

        public BanModel(DomusDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public long TargetUserId { get; set; }

        public User? TargetUser { get; set; }

        [BindProperty]
        public bool PermaBan { get; set; }

        [BindProperty]
        public DateTime? EndDate { get; set; }

        [BindProperty]
        public string? Reason { get; set; }

        public async Task<IActionResult> OnGetAsync(long userId)
        {
            TargetUser = await _context.Users.Include(u => u.Ban).FirstOrDefaultAsync(u => u.Id == userId);
            if (TargetUser == null)
            {
                return NotFound();
            }

            TargetUserId = userId;

            if (TargetUser.Ban != null)
            {
                PermaBan = TargetUser.Ban.PermaBan;
                EndDate = TargetUser.Ban.EndDate;
                Reason = TargetUser.Ban.Reason;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _context.Users.Include(u => u.Ban).FirstOrDefaultAsync(u => u.Id == TargetUserId);
            if (user == null)
            {
                return NotFound();
            }

            TargetUser = user; // Populate for View in case of errors

            // 1. Hierarchy Check - Self Ban
            var currentUserIdStr = User.FindFirst("UserId")?.Value;
            if (long.TryParse(currentUserIdStr, out long currentUserId) && currentUserId == TargetUserId)
            {
                ModelState.AddModelError(string.Empty, "Security Alert: You cannot ban yourself.");
                return Page();
            }

            // 2. Hierarchy Check - Role Protection
            var targetRoles = user.Roles ?? new System.Collections.Generic.List<string>();

            // Protection 1: No one can ban a Rex (Supreme Admin) via this UI
            if (targetRoles.Contains("Rex", StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "Security Alert: You cannot ban a Rex user.");
                return Page();
            }

            // Protection 2: Moderators cannot ban other Moderators
            // Only Rex can ban Moderators
            if (targetRoles.Contains("Moderator", StringComparer.OrdinalIgnoreCase) && !User.IsInRole("Rex"))
            {
                ModelState.AddModelError(string.Empty, "Security Alert: You do not have permission to ban a Moderator.");
                return Page();
            }

            // 3. Logic Consistency: PermaBan vs Date
            if (PermaBan)
            {
                EndDate = null; // Enforce null if Permanent
            }
            else
            {
                // 4. Validation: Past Date
                if (!EndDate.HasValue)
                {
                    ModelState.AddModelError("EndDate", "End Date is required for temporary bans.");
                    return Page();
                }

                if (EndDate.Value <= DateTime.UtcNow)
                {
                    ModelState.AddModelError("EndDate", "End Date must be in the future.");
                    return Page();
                }
            }

            if (user.Ban == null)
            {
                user.Ban = new Ban
                {
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow // 5. Set CreatedAt
                };
            }

            user.Ban.PermaBan = PermaBan;
            user.Ban.EndDate = EndDate;
            user.Ban.Reason = Reason;
            user.Ban.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostUnbanAsync(long userId)
        {
             var user = await _context.Users.Include(u => u.Ban).FirstOrDefaultAsync(u => u.Id == userId);
             if (user == null || user.Ban == null)
             {
                 return RedirectToPage("./Index");
             }

             // Hierarchy Checks
             var targetRoles = user.Roles ?? new System.Collections.Generic.List<string>();

             // Protection 1: Cannot unban a Rex (if somehow banned)
             if (targetRoles.Contains("Rex"))
             {
                 TargetUserId = userId;
                 TargetUser = user;
                 if (user.Ban != null) { PermaBan = user.Ban.PermaBan; EndDate = user.Ban.EndDate; Reason = user.Ban.Reason; }
                 
                 ModelState.AddModelError(string.Empty, "Security Alert: You cannot manage a Rex user.");
                 return Page();
             }

             // Protection 2: Moderators cannot unban other Moderators
             if (targetRoles.Contains("Moderator") && !User.IsInRole("Rex"))
             {
                 TargetUserId = userId;
                 TargetUser = user;
                 if (user.Ban != null) { PermaBan = user.Ban.PermaBan; EndDate = user.Ban.EndDate; Reason = user.Ban.Reason; }

                 ModelState.AddModelError(string.Empty, "Security Alert: You do not have permission to unban a Moderator.");
                 return Page();
             }

             _context.Remove(user.Ban);
             await _context.SaveChangesAsync();
             
             return RedirectToPage("./Index");
        }
    }
}
