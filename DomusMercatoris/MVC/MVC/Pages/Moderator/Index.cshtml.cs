using System.Collections.Generic;
using System.Linq;
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
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<User> Users { get; set; } = new List<User>();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Users.Include(u => u.Ban).AsQueryable();

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var s = Search.ToLower().Trim();
                query = query.Where(u => 
                    u.Email.ToLower().Contains(s) || 
                    u.FirstName.ToLower().Contains(s) || 
                    u.LastName.ToLower().Contains(s));
            }

            Users = await query.OrderBy(u => u.Email).Take(50).ToListAsync();
        }
    }
}
