using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatorisDotnetMVC.Services;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Core.Constants;
using System.Threading.Tasks;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Roles = AppConstants.Roles.User + "," + AppConstants.Roles.Manager)]
    public class SearchModel : PageModel
    {
        private readonly ProductService _productService;
        private readonly UserService _userService;
        public SearchModel(ProductService productService, UserService userService)
        {
            _productService = productService;
            _userService = userService;
        }

        public string Query { get; set; } = string.Empty;
        public string Context { get; set; } = "global";
        public List<Product> ProductResults { get; set; } = new();
        public List<User> WorkerResults { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            Query = (Request.Query["q"].ToString() ?? string.Empty).Trim();
            Context = (Request.Query["ctx"].ToString() ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(Context)) Context = "global";

            var comp = User.FindFirst(AppConstants.CustomClaimTypes.CompanyId)?.Value;
            int companyId = (!string.IsNullOrEmpty(comp) && int.TryParse(comp, out var c)) ? c : 0;
            if (companyId == 0)
            {
                var idClaim = User.FindFirst(AppConstants.CustomClaimTypes.UserId)?.Value;
                if (!string.IsNullOrEmpty(idClaim) && long.TryParse(idClaim, out var userId))
                {
                    var me = await _userService.GetByIdAsync(userId);
                    if (me != null) companyId = me.CompanyId ?? 0;
                }
            }

            if (!string.IsNullOrEmpty(Query))
            {
                if (Context == "products")
                {
                    ProductResults = await _productService.SearchByCompanyAsync(companyId, Query, 20);
                }
                else if (Context == "workers")
                {
                    WorkerResults = await _userService.SearchByCompanyAsync(companyId, Query, 20);
                }
                else
                {
                    ProductResults = await _productService.SearchByCompanyAsync(companyId, Query, 10);
                    WorkerResults = await _userService.SearchByCompanyAsync(companyId, Query, 10);
                }
            }
            return Page();
        }
    }
}
