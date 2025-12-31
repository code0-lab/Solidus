using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatorisDotnetMVC.Services;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Service.Services;
using DomusMercatoris.Service.DTOs;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Roles = "User,Manager")]
    public class ProductDetailModel : PageModel
    {
        private readonly ProductService _productService;
        private readonly VariantProductService _variantService;

        public ProductDetailModel(ProductService productService, VariantProductService variantService)
        {
            _productService = productService;
            _variantService = variantService;
        }

        public Product? Product { get; set; }
        public List<VariantProductDto> Variants { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long id)
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

            Variants = await _variantService.GetVariantsByProductIdAsync(id);

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
