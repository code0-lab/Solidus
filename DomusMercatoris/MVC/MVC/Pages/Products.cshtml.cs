using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatorisDotnetMVC.Services;
using DomusMercatoris.Core.Entities;
using System.Threading.Tasks;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Roles = "User,Manager")]
    public class ProductsModel : PageModel
    {
        private readonly ProductService _productService;
        private readonly UserService _userService;
        public ProductsModel(ProductService productService, UserService userService)
        {
            _productService = productService;
            _userService = userService;
        }

        public List<Product> Items { get; set; } = new();
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 9;
        public int TotalCount { get; set; } = 0;
        public int TotalPages { get; set; } = 1;

        public async Task<IActionResult> OnGetAsync()
        {
            var pageStr = Request.Query["page"].ToString();
            if (!string.IsNullOrEmpty(pageStr) && int.TryParse(pageStr, out var p))
            {
                PageNumber = Math.Max(1, p);
            }
            var comp = User.FindFirst("CompanyId")?.Value;
            if (!string.IsNullOrEmpty(comp) && int.TryParse(comp, out var companyId))
            {
                TotalCount = await _productService.CountByCompanyAsync(companyId);
                TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
                if (PageNumber > TotalPages) PageNumber = TotalPages;
                Items = await _productService.GetByCompanyPageAsync(companyId, PageNumber, PageSize);
            }
            else
            {
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
                TotalCount = await _productService.CountByCompanyAsync(me.CompanyId);
                TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
                if (PageNumber > TotalPages) PageNumber = TotalPages;
                Items = await _productService.GetByCompanyPageAsync(me.CompanyId, PageNumber, PageSize);
            }
            return Page();
        }

        public IActionResult OnPostDelete(long id, int? page)
        {
            var comp = User.FindFirst("CompanyId")?.Value;
            int companyId = (!string.IsNullOrEmpty(comp) && int.TryParse(comp, out var c)) ? c : 0;
            var ok = _productService.Delete(id);
            TempData["Message"] = ok ? "Product deleted." : "Product not found.";
            var p = page.HasValue && page.Value > 0 ? page.Value : 1;
            return Redirect($"/Products?page={p}");
        }
    }
}
