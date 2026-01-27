using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatoris.Core.Entities;
using DomusMercatorisDotnetMVC.Services;

namespace DomusMercatorisDotnetMVC.Pages.Moderator
{
    [Authorize(Roles = "Moderator,Rex")]
    public class IndexModel : PageModel
    {
        private readonly UserService _userService;

        public IndexModel(UserService userService)
        {
            _userService = userService;
        }

        public List<User> Users { get; set; } = new List<User>();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        public async Task OnGetAsync()
        {
            Users = await _userService.SearchUsersAsync(Search);
        }
    }
}
