using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatorisDotnetMVC.Services;
using DomusMercatoris.Core.Entities;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Policy = "WorkersAccess")]
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
        public Dictionary<long, HashSet<string>> UserPageAccessMap { get; set; } = new();

        public static readonly Dictionary<string, Dictionary<string, string>> PermissionGroups = new()
        {
            { "Catalog Management", new Dictionary<string, string> 
                { 
                    { "Products", "Manage Products" },
                    { "Categories", "Manage Categories" },
                    { "Brands", "Manage Brands" }
                } 
            },
            { "Sales & Orders", new Dictionary<string, string> 
                { 
                    { "Orders", "Manage Orders" },
                    { "Customers", "Manage Customers" },
                    { "ManageCargos", "Manage Cargo Settings" },
                    { "Refunds", "Manage Refunds" }
                } 
            },
            { "Marketing", new Dictionary<string, string> 
                { 
                    { "CompanyBanners", "Manage Banners (AI)" }
                } 
            },
            { "Administration", new Dictionary<string, string> 
                { 
                    { "Workers", "Manage Workforce" }
                } 
            },
            { "Task Management", new Dictionary<string, string> 
                { 
                    { "Tasks", "Manage All Tasks" }
                } 
            }
        };

        public List<string> PageAccessKeys => PermissionGroups.SelectMany(g => g.Value.Keys).ToList();

        public async Task<IActionResult> OnGetAsync()
        {
            var comp = User.FindFirst("CompanyId")?.Value;
            if (!string.IsNullOrEmpty(comp) && int.TryParse(comp, out var companyId))
            {
                var workers = await _userService.GetByCompanyAsync(companyId);
                Workers = workers
                    .Where(u => !(u.Roles ?? new List<string>()).Any(r => 
                        string.Equals(r, "Customer", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(r, "Rex", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(r, "Moderator", StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                await LoadPageAccessAsync(companyId);
                return Page();
            }
            var idClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(idClaim) || !long.TryParse(idClaim, out var userId))
            {
                return RedirectToPage("/Index");
            }
            var me = await _userService.GetByIdAsync(userId);
            if (me == null)
            {
                return RedirectToPage("/Index");
            }
            var workersMe = await _userService.GetByCompanyAsync(me.CompanyId);
            Workers = workersMe
                .Where(u => !(u.Roles ?? new List<string>()).Any(r => 
                    string.Equals(r, "Customer", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(r, "Rex", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(r, "Moderator", StringComparison.OrdinalIgnoreCase)))
                .ToList();
            await LoadPageAccessAsync(me.CompanyId);
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            if (!User.IsInRole("Manager"))
            {
                ModelState.AddModelError(string.Empty, "Unauthorized");
                return await OnGetAsync();
            }
            var comp = User.FindFirst("CompanyId")?.Value;
            int companyId;
            if (!string.IsNullOrEmpty(comp) && int.TryParse(comp, out companyId))
            {
                var ok1 = await _userService.UpdateUserInCompanyAsync(Edit.Id, companyId, Edit.FirstName, Edit.LastName, Edit.Email);
                if (!ok1)
                {
                    ModelState.AddModelError(string.Empty, "Update failed");
                }
                return await OnGetAsync();
            }
            var idClaim = User.FindFirst("UserId")?.Value;
            long userId;
            if (string.IsNullOrEmpty(idClaim) || !long.TryParse(idClaim, out userId))
            {
                ModelState.AddModelError(string.Empty, "Unauthorized");
                return await OnGetAsync();
            }
            if (!ModelState.IsValid)
            {
                return await OnGetAsync();
            }
            var me = await _userService.GetByIdAsync(userId);
            if (me == null)
            {
                ModelState.AddModelError(string.Empty, "Unauthorized");
                return await OnGetAsync();
            }
            var ok = await _userService.UpdateUserInCompanyAsync(Edit.Id, me.CompanyId, Edit.FirstName, Edit.LastName, Edit.Email);
            if (!ok)
            {
                ModelState.AddModelError(string.Empty, "Update failed");
            }
            return await OnGetAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            if (!User.IsInRole("Manager"))
            {
                ModelState.AddModelError(string.Empty, "Unauthorized");
                return await OnGetAsync();
            }
            var comp = User.FindFirst("CompanyId")?.Value;
            int companyId;
            var idClaim = User.FindFirst("UserId")?.Value;
            long currentUserId = 0;
            if (!string.IsNullOrEmpty(idClaim)) long.TryParse(idClaim, out currentUserId);

            if (!string.IsNullOrEmpty(comp) && int.TryParse(comp, out companyId))
            {
                var ok1 = await _userService.DeleteUserInCompanyAsync(DeleteId, companyId, currentUserId);
                if (!ok1)
                {
                    ModelState.AddModelError(string.Empty, "Delete failed");
                }
                return await OnGetAsync();
            }
            // var idClaim = User.FindFirst("UserId")?.Value; // reused
            long userId;
            if (string.IsNullOrEmpty(idClaim) || !long.TryParse(idClaim, out userId))
            {
                ModelState.AddModelError(string.Empty, "Unauthorized");
                return await OnGetAsync();
            }
            if (!ModelState.IsValid)
            {
                return await OnGetAsync();
            }
            var me = await _userService.GetByIdAsync(userId);
            if (me == null)
            {
                ModelState.AddModelError(string.Empty, "Unauthorized");
                return await OnGetAsync();
            }
            var ok = await _userService.DeleteUserInCompanyAsync(DeleteId, me.CompanyId, userId);
            if (!ok)
            {
                ModelState.AddModelError(string.Empty, "Delete failed");
            }
            return await OnGetAsync();
        }

        public async Task<JsonResult> OnGetCheckUserTasksAsync(long userId)
        {
            var comp = User.FindFirst("CompanyId")?.Value;
            if (string.IsNullOrEmpty(comp) || !int.TryParse(comp, out var companyId))
            {
                 var idClaim = User.FindFirst("UserId")?.Value;
                 if (string.IsNullOrEmpty(idClaim) || !long.TryParse(idClaim, out var uId)) return new JsonResult(new { count = 0 });
                 
                 var me = await _userService.GetByIdAsync(uId);
                 if (me == null) return new JsonResult(new { count = 0 });
                 companyId = me.CompanyId;
            }

            // Check only assigned tasks as requested
            var workers = await _userService.GetByCompanyAsync(companyId);
            if(!workers.Any(w => w.Id == userId)) return new JsonResult(new { count = 0 });

            // We can use TaskService or DbContext. Let's use TaskService if available, but it's not injected here.
            // Wait, TaskService is NOT injected. I should inject it or use _userService to access context?
            // _userService has _dbContext but it's private.
            // I should inject TaskService or use _userService to add a helper method.
            // Actually, I can just add a helper in UserService: GetPendingTaskCountForUserAsync
            
            // Let's assume I'll add GetPendingTaskCountAsync to UserService for simplicity here
            var count = await _userService.GetPendingTaskCountAsync(userId);
            return new JsonResult(new { count });
        }

        public async Task<IActionResult> OnPostUpdatePermissionsAsync(long userId, List<string> pageKeys)
        {
            if (!User.IsInRole("Manager"))
            {
                ModelState.AddModelError(string.Empty, "Unauthorized");
                return await OnGetAsync();
            }

            var comp = User.FindFirst("CompanyId")?.Value;
            int companyId = 0;
            if (!string.IsNullOrEmpty(comp) && int.TryParse(comp, out var cid))
            {
                companyId = cid;
            }
            else
            {
                var idClaim = User.FindFirst("UserId")?.Value;
                if (!string.IsNullOrEmpty(idClaim) && long.TryParse(idClaim, out var currentUserId))
                {
                    var me = await _userService.GetByIdAsync(currentUserId);
                    if (me != null) companyId = me.CompanyId;
                }
            }

            if (companyId == 0)
            {
                ModelState.AddModelError(string.Empty, "Authorization error");
                return await OnGetAsync();
            }

            var allowed = new HashSet<string>(PageAccessKeys, StringComparer.OrdinalIgnoreCase);
            var filtered = (pageKeys ?? new List<string>()).Where(k => allowed.Contains(k)).ToList();

            await _userService.UpdateUserPageAccessAsync(companyId, userId, filtered);
            return await OnGetAsync();
        }

        private async Task LoadPageAccessAsync(int companyId)
        {
            var accesses = await _userService.GetUserPageAccessesForCompanyAsync(companyId);
            var map = new Dictionary<long, HashSet<string>>();
            foreach (var group in accesses.GroupBy(a => a.UserId))
            {
                map[group.Key] = new HashSet<string>(group.Select(a => a.PageKey), StringComparer.OrdinalIgnoreCase);
            }
            UserPageAccessMap = map;
        }
    }
}
