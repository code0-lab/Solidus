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

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public async Task OnGetAsync()
        {
            if (PageIndex < 1) PageIndex = 1;
            int pageSize = 10;
            var result = await _userService.SearchUsersPagedAsync(Search, PageIndex, pageSize);
            Users = result.Users;
            TotalPages = (int)System.Math.Ceiling(result.TotalCount / (double)pageSize);
        }
    }
}
