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

            var companyId = await _userService.GetCompanyIdFromUserAsync(User);
            if (companyId <= 0)
            {
                return RedirectToPage("/Index");
            }

            var result = await _productService.GetPagedByCompanyAsync(companyId, PageNumber, PageSize);
            Items = result.Items;
            TotalCount = result.TotalCount;
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
            
            if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;
            
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id, int? page)
        {
            var ok = await _productService.DeleteAsync(id);
            TempData["Message"] = ok ? "Product deleted." : "Product not found.";
            var p = page.HasValue && page.Value > 0 ? page.Value : 1;
            return Redirect($"/Products?page={p}");
        }
    }
}
