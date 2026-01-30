using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatorisDotnetMVC.Services;
using DomusMercatoris.Core.Constants;

namespace DomusMercatorisDotnetMVC.Pages.Moderator
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        private readonly UserService _userService;

        public LoginModel(UserService userService)
        {
            _userService = userService;
        }

        public void OnGet()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                 // If already logged in as moderator or rex, redirect to index
                 if (User.IsInRole(AppConstants.Roles.Moderator) || User.IsInRole(AppConstants.Roles.Rex))
                 {
                     Response.Redirect("/Moderator/Index");
                 }
            }
        }

        public async Task<IActionResult> OnPost()
        {
            var user = await _userService.UserLoginAsync(Email, Password);
            if (user != null)
            {
                // Check for Moderator or Rex role
                if (user.Roles == null || (!user.Roles.Any(r => r.Trim().Equals(AppConstants.Roles.Moderator, StringComparison.OrdinalIgnoreCase)) && !user.Roles.Any(r => r.Trim().Equals(AppConstants.Roles.Rex, StringComparison.OrdinalIgnoreCase))))
                {
                    ModelState.AddModelError(string.Empty, "Access Denied: You do not have Moderator privileges.");
                    return Page();
                }

                // Standard Login Logic
                var fullName = $"{user.FirstName} {user.LastName}";
                var claims = new List<Claim>
                {
                    new Claim(AppConstants.CustomClaimTypes.UserId, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, fullName)
                };

                var roles = (user.Roles ?? new List<string>()).ToList();
                
                // If user is banned, ONLY assign "Baned" role
                if (user.Ban != null && user.Ban.IsBanned)
                {
                    claims.Add(new Claim(ClaimTypes.Role, AppConstants.Roles.Banned));
                    HttpContext.Session.SetString(AppConstants.SessionKeys.Role, AppConstants.Roles.Banned);
                }
                else
                {
                    foreach (var r in roles.Select(r => r.Trim()).Distinct())
                    {
                        claims.Add(new Claim(ClaimTypes.Role, r));
                    }
                    HttpContext.Session.SetString(AppConstants.SessionKeys.Role, string.Join(",", roles));
                }

                if (user.CompanyId.HasValue)
                {
                    claims.Add(new Claim(AppConstants.CustomClaimTypes.CompanyId, user.CompanyId.Value.ToString()));
                }

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                
                HttpContext.Session.SetString(AppConstants.SessionKeys.Email, user.Email);

                // Ban check (even though moderators probably shouldn't be banned, good to keep consistent or maybe moderators are immune? 
                // The user requirement says "Baned users... redirected to Baned page". 
                // It doesn't explicitly say moderators are immune, but usually they are.
                // However, if a moderator IS banned, they probably shouldn't be accessing the panel.
                if (user.Ban != null && user.Ban.IsBanned)
                {
                     return RedirectToPage("/Banned");
                }

                return RedirectToPage("/Moderator/Index");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "E-mail or Password is incorrect.");
                return Page();
            }
        }
    }
}
