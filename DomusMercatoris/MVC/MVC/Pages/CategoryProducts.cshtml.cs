using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatoris.Core.Entities;
using DomusMercatorisDotnetMVC.Services;
using System.Linq;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Roles = "Manager,User")]
    public class CategoryProductsModel : PageModel
    {
        private readonly ProductService _productService;
        private readonly UserService _userService;

        public CategoryProductsModel(ProductService productService, UserService userService)
        {
            _productService = productService;
            _userService = userService;
        }

        public List<Product> Items { get; set; } = new();
        public string CategoryName { get; set; } = string.Empty;
        public int CategoryId { get; set; } = 0;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 9;
        public int TotalCount { get; set; } = 0;
        public int TotalPages { get; set; } = 1;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            int companyId = await _userService.GetCompanyIdFromUserAsync(User);
            
            var cat = await _productService.GetCategoryByIdAsync(companyId, id);
            if (cat == null)
            {
                return RedirectToPage("/Categories");
            }
            CategoryId = id;
            CategoryName = cat.Name ?? string.Empty;
            
            var pageStr = Request.Query["page"].ToString();
            if (!string.IsNullOrEmpty(pageStr) && int.TryParse(pageStr, out var p))
            {
                PageNumber = Math.Max(1, p);
            }

            var result = await _productService.GetPagedByCategoryAsync(companyId, id, PageNumber, PageSize);
            
            Items = result.Items;
            TotalCount = result.TotalCount;
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
            
            if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;
            
            return Page();
        }
    }
}
