using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatorisDotnetMVC.Services;
using DomusMercatorisDotnetMVC.Models;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Roles = "User,Manager")]
    public class ProductDetailModel : PageModel
    {
        private readonly ProductService _productService;
        public ProductDetailModel(ProductService productService)
        {
            _productService = productService;
        }

        public Product? Product { get; set; }

        public IActionResult OnGet(long id)
        {
            var comp = User.FindFirst("CompanyId")?.Value;
            if (string.IsNullOrEmpty(comp) || !int.TryParse(comp, out var companyId))
            {
                return RedirectToPage("/Products");
            }
            Product = _productService.GetByIdInCompany(id, companyId);
            if (Product == null)
            {
                return RedirectToPage("/Products");
            }
            return Page();
        }

        public IActionResult OnPostDelete(long id)
        {
            var ok = _productService.Delete(id);
            TempData["Message"] = ok ? "Product deleted." : "Product not found.";
            return RedirectToPage("/Products");
        }
    }
}
