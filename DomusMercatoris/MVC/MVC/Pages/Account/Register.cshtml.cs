using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatorisDotnetMVC.Dto.UserDto;
using DomusMercatoris.Core.Entities;
using DomusMercatorisDotnetMVC.Services;

namespace DomusMercatorisDotnetMVC.Pages.Account
{
    public class RegisterModel : PageModel
    {
        [BindProperty]
        public UserRegisterDto UserRegisterDto { get; set; } = new();

        [BindProperty]
        public bool IsAutoGeneratePassword { get; set; } = true;

        private readonly UserService _userService;
        public RegisterModel(UserService userService)
        {
            _userService = userService;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            string? generatedPassword = null;

            if (IsAutoGeneratePassword)
            {
                // Generate a random password compliant with regex: ^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$ and length 5-12
                generatedPassword = GenerateRandomPassword();
                UserRegisterDto.Password = generatedPassword;
                
                // Clear validation errors for Password since we just set it programmatically
                ModelState.Remove("UserRegisterDto.Password");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }
            if (string.IsNullOrWhiteSpace(UserRegisterDto.CompanyName))
            {
                ModelState.AddModelError(string.Empty, "Company Name is required.");
                return Page();
            }
            User user = await _userService.UserRegisterAsync(UserRegisterDto);
            if (user != null)
            {
                if (IsAutoGeneratePassword)
                {
                    TempData["GeneratedPassword"] = generatedPassword;
                }
                else
                {
                    TempData["RegistrationSuccess"] = true;
                }
                return Page();
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Registration failed.");
                return Page();
            }
        }

        private string GenerateRandomPassword()
        {
            const string lowers = "abcdefghijklmnopqrstuvwxyz";
            const string uppers = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string all = lowers + uppers + digits;
            
            var random = new Random();
            var password = new char[10]; // Length 10 (between 5 and 12)

            // Ensure requirements
            password[0] = lowers[random.Next(lowers.Length)];
            password[1] = uppers[random.Next(uppers.Length)];
            password[2] = digits[random.Next(digits.Length)];

            // Fill the rest
            for (int i = 3; i < password.Length; i++)
            {
                password[i] = all[random.Next(all.Length)];
            }

            // Shuffle
            return new string(password.OrderBy(x => random.Next()).ToArray());
        }
    }
}
