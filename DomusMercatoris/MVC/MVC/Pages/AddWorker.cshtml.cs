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

        [BindProperty]
        public bool IsAutoGeneratePassword { get; set; } = true;

        private readonly UserService _userService;
        public AddWorkerModel(UserService userService)
        {
            _userService = userService;
        }

        public void OnGet() {}

        public async Task<IActionResult> OnPostAsync()
        {
            string? generatedPassword = null;

            if (IsAutoGeneratePassword)
            {
                generatedPassword = GenerateRandomPassword();
                UserRegisterDto.Password = generatedPassword;
                ModelState.Remove("UserRegisterDto.Password");
            }

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
                var manager = await _userService.GetByIdAsync(managerId);
                if (manager == null)
                {
                    ModelState.AddModelError(string.Empty, "Manager not found.");
                    return Page();
                }
                companyId = manager.CompanyId;
            }
            var user = await _userService.RegisterWorkerAsync(UserRegisterDto, companyId);
            if (user != null)
            {
                if (IsAutoGeneratePassword)
                {
                    TempData["GeneratedPassword"] = generatedPassword;
                    TempData["WorkerEmail"] = user.Email;
                    // Don't redirect immediately if we want to show the password
                    return Page();
                }
                else
                {
                    TempData["Message"] = "Worker added.";
                    return RedirectToPage("/Dashboard");
                }
            }
            ModelState.AddModelError(string.Empty, "Registration failed.");
            return Page();
        }

        private string GenerateRandomPassword()
        {
            const string lowers = "abcdefghijklmnopqrstuvwxyz";
            const string uppers = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string all = lowers + uppers + digits;
            
            var random = new Random();
            var password = new char[10];

            password[0] = lowers[random.Next(lowers.Length)];
            password[1] = uppers[random.Next(uppers.Length)];
            password[2] = digits[random.Next(digits.Length)];

            for (int i = 3; i < password.Length; i++)
            {
                password[i] = all[random.Next(all.Length)];
            }

            return new string(password.OrderBy(x => random.Next()).ToArray());
        }
    }
}
