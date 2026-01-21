using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatorisDotnetMVC.Services;

namespace DomusMercatorisDotnetMVC.Pages
{
    public class IndexModel : PageModel
    {

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
         public string Password { get; set; } = string.Empty;

        private readonly UserService _userService;
         public IndexModel(UserService userService)
        {
            _userService = userService;
        }

        public void OnGet()
        {
            Console.WriteLine("Razor Pages Login GET");
        }

        public async Task<IActionResult> OnPost()
        {
            var user = await _userService.UserLoginAsync(Email, Password);
            if (user != null) {
                var fullName = $"{user.FirstName} {user.LastName}";
                var claims = new List<Claim>
                {
                    new Claim("UserId", user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, fullName)
                };

                var roles = (user.Roles ?? new List<string>()).ToList();
                
                // If user is banned, ONLY assign "Baned" role
                if (user.Ban != null && user.Ban.IsBaned)
                {
                    claims.Add(new Claim(ClaimTypes.Role, "Baned"));
                    HttpContext.Session.SetString("Role", "Baned");
                }
                else 
                {
                    if (roles.Count == 0) {
                        roles.Add("User");
                    }
                    var hasManager = roles.Any(r => string.Equals(r, "Manager", StringComparison.OrdinalIgnoreCase));
                    var hasUser = roles.Any(r => string.Equals(r, "User", StringComparison.OrdinalIgnoreCase));
                    if (hasManager && !hasUser) {
                        roles.Add("User");
                    }
                    foreach (var r in roles.Select(r => r.Trim()).Distinct())
                    {
                        claims.Add(new Claim(ClaimTypes.Role, r));
                    }
                    HttpContext.Session.SetString("Role", string.Join(",", roles));
                }

                claims.Add(new Claim("CompanyId", user.CompanyId.ToString()));

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                
                HttpContext.Session.SetString("Email", user.Email);

                if (user.Ban != null && user.Ban.IsBaned)
                {
                    return RedirectToPage("/Baned");
                }

                return RedirectToPage("/Dashboard");
            } else {
                ModelState.AddModelError(string.Empty, "E-mail or Password is incorrect.");
                return Page();
            }
        }

        // /Index?handler=Logout
        public async Task<IActionResult> OnGetLogout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToPage("/Index"); 
        }

    }
}
