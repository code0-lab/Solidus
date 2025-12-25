using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DomusMercatorisDotnetMVC.Models;
using DomusMercatorisDotnetMVC.Utils;

namespace DomusMercatorisDotnetMVC.Pages.Moderator
{
    [Authorize(Roles = "Moderator")]
    public class BanModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BanModel(ApplicationDbContext context)
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

            if (user.Ban == null)
            {
                user.Ban = new Ban
                {
                    UserId = user.Id
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
             if (user != null && user.Ban != null)
             {
                 _context.Remove(user.Ban);
                 await _context.SaveChangesAsync();
             }
             return RedirectToPage("./Index");
        }
    }
}
