using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatorisDotnetMVC.Dto.UserDto;
using DomusMercatorisDotnetMVC.Services;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Roles = "Manager")]
    public class AddWorkerModel : PageModel
    {
        [BindProperty]
        public UserRegisterDto UserRegisterDto { get; set; } = new();

        private readonly UserService _userService;
        public AddWorkerModel(UserService userService)
        {
            _userService = userService;
        }

        public void OnGet() {}

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            var comp = User.FindFirst("CompanyId")?.Value;
            int companyId;
            if (string.IsNullOrEmpty(comp) || !int.TryParse(comp, out companyId))
            {
                var idClaim = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(idClaim) || !long.TryParse(idClaim, out var managerId))
                {
                    ModelState.AddModelError(string.Empty, "Authorization error.");
                    return Page();
                }
                var manager = _userService.GetById(managerId);
                if (manager == null)
                {
                    ModelState.AddModelError(string.Empty, "Manager not found.");
                    return Page();
                }
                companyId = manager.CompanyId;
            }
            var user = _userService.RegisterWorker(UserRegisterDto, companyId);
            if (user != null)
            {
                TempData["Message"] = "Worker added.";
                return RedirectToPage("/Dashboard");
            }
            ModelState.AddModelError(string.Empty, "Registration failed.");
            return Page();
        }
    }
}
