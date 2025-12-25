using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatorisDotnetMVC.Services;
using DomusMercatoris.Core.Entities;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Roles = "User,Manager")]
    public class WorkersModel : PageModel
    {
        public class EditInput
        {
            public long Id { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }

        [BindProperty]
        public EditInput Edit { get; set; } = new();

        [BindProperty]
        public long DeleteId { get; set; }

        private readonly UserService _userService;
        public WorkersModel(UserService userService)
        {
            _userService = userService;
        }

        public List<User> Workers { get; set; } = new();

        public IActionResult OnGet()
        {
            var comp = User.FindFirst("CompanyId")?.Value;
            if (!string.IsNullOrEmpty(comp) && int.TryParse(comp, out var companyId))
            {
                Workers = _userService.GetByCompany(companyId)
                    .Where(u => !(u.Roles ?? new List<string>()).Any(r => string.Equals(r, "Customer", StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                return Page();
            }
            var idClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(idClaim) || !long.TryParse(idClaim, out var userId))
            {
                return RedirectToPage("/Index");
            }
            var me = _userService.GetById(userId);
            if (me == null)
            {
                return RedirectToPage("/Index");
            }
            Workers = _userService.GetByCompany(me.CompanyId)
                .Where(u => !(u.Roles ?? new List<string>()).Any(r => string.Equals(r, "Customer", StringComparison.OrdinalIgnoreCase)))
                .ToList();
            return Page();
        }

        public IActionResult OnPostUpdate()
        {
            if (!User.IsInRole("Manager"))
            {
                ModelState.AddModelError(string.Empty, "Unauthorized");
                return OnGet();
            }
            var comp = User.FindFirst("CompanyId")?.Value;
            int companyId;
            if (!string.IsNullOrEmpty(comp) && int.TryParse(comp, out companyId))
            {
                var ok1 = _userService.UpdateUserInCompany(Edit.Id, companyId, Edit.FirstName, Edit.LastName, Edit.Email);
                if (!ok1)
                {
                    ModelState.AddModelError(string.Empty, "Update failed");
                }
                return OnGet();
            }
            var idClaim = User.FindFirst("UserId")?.Value;
            long userId;
            if (string.IsNullOrEmpty(idClaim) || !long.TryParse(idClaim, out userId))
            {
                ModelState.AddModelError(string.Empty, "Unauthorized");
                return OnGet();
            }
            if (!ModelState.IsValid)
            {
                return OnGet();
            }
            var me = _userService.GetById(userId);
            if (me == null)
            {
                ModelState.AddModelError(string.Empty, "Unauthorized");
                return OnGet();
            }
            var ok = _userService.UpdateUserInCompany(Edit.Id, me.CompanyId, Edit.FirstName, Edit.LastName, Edit.Email);
            if (!ok)
            {
                ModelState.AddModelError(string.Empty, "Update failed");
            }
            return OnGet();
        }

        public IActionResult OnPostDelete()
        {
            if (!User.IsInRole("Manager"))
            {
                ModelState.AddModelError(string.Empty, "Unauthorized");
                return OnGet();
            }
            var comp = User.FindFirst("CompanyId")?.Value;
            int companyId;
            if (!string.IsNullOrEmpty(comp) && int.TryParse(comp, out companyId))
            {
                var ok1 = _userService.DeleteUserInCompany(DeleteId, companyId);
                if (!ok1)
                {
                    ModelState.AddModelError(string.Empty, "Delete failed");
                }
                return OnGet();
            }
            var idClaim = User.FindFirst("UserId")?.Value;
            long userId;
            if (string.IsNullOrEmpty(idClaim) || !long.TryParse(idClaim, out userId))
            {
                ModelState.AddModelError(string.Empty, "Unauthorized");
                return OnGet();
            }
            if (!ModelState.IsValid)
            {
                return OnGet();
            }
            var me = _userService.GetById(userId);
            if (me == null)
            {
                ModelState.AddModelError(string.Empty, "Unauthorized");
                return OnGet();
            }
            var ok = _userService.DeleteUserInCompany(DeleteId, me.CompanyId);
            if (!ok)
            {
                ModelState.AddModelError(string.Empty, "Delete failed");
            }
            return OnGet();
        }
    }
}
