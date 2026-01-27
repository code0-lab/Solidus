using DomusMercatoris.Core.Entities;
using DomusMercatoris.Service.DTOs;
using DomusMercatoris.Service.Services;
using DomusMercatorisDotnetMVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatoris.Data;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Policy = "ProductsAccess")]
    public class ManageVariantsModel : PageModel
    {
        private readonly VariantProductService _variantService;
        private readonly ProductService _productService;
        private readonly IWebHostEnvironment _env;
        private readonly UserService _userService;

        public ManageVariantsModel(VariantProductService variantService, ProductService productService, IWebHostEnvironment env, UserService userService)
        {
            _variantService = variantService;
            _productService = productService;
            _env = env;
            _userService = userService;
        }

        public Product? Product { get; set; }
        public List<VariantProductDto> Variants { get; set; } = new();

        [BindProperty]
        public CreateVariantProductDto NewVariant { get; set; } = new();

        [BindProperty]
        public UpdateVariantProductDto UpdateVariant { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long productId)
        {
            var companyId = await _userService.GetCompanyIdFromUserAsync(User);
            if (companyId == 0)
            {
                return RedirectToPage("/Products");
            }

            Product = await _productService.GetByIdInCompanyAsync(productId, companyId);
            if (Product == null)
            {
                return RedirectToPage("/Products");
            }

            Variants = await _variantService.GetVariantsByProductIdAsync(productId);
            NewVariant.ProductId = productId;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long productId)
        {
            var companyId = await _userService.GetCompanyIdFromUserAsync(User);
            if (companyId == 0)
            {
                return RedirectToPage("/Products");
            }

            Product = await _productService.GetByIdInCompanyAsync(productId, companyId);
            if (Product == null)
            {
                return RedirectToPage("/Products");
            }

            // Clear all validation state to avoid interference from UpdateVariant model
            ModelState.Clear();

            // Re-validate ONLY NewVariant
            if (!TryValidateModel(NewVariant, nameof(NewVariant)))
            {
                 Variants = await _variantService.GetVariantsByProductIdAsync(productId);
                 return Page();
            }

            try
            {
                NewVariant.ProductId = productId;

                if (!NewVariant.IsCustomizable)
                {
                    NewVariant.Price = Product.Price;
                }

                await _variantService.CreateVariantAsync(NewVariant);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                Variants = await _variantService.GetVariantsByProductIdAsync(productId);
                return Page();
            }

            return RedirectToPage(new { productId });
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
             var companyId = await _userService.GetCompanyIdFromUserAsync(User);
            if (companyId == 0)
            {
                return RedirectToPage("/Products");
            }
            
            Product = await _productService.GetByIdInCompanyAsync(UpdateVariant.ProductId, companyId);
            if (Product == null)
            {
                return RedirectToPage("/Products");
            }

            // Clear all validation state to avoid interference from NewVariant model
            ModelState.Clear();

            // Re-validate ONLY UpdateVariant
            if (!TryValidateModel(UpdateVariant, nameof(UpdateVariant)))
            {
                Variants = await _variantService.GetVariantsByProductIdAsync(UpdateVariant.ProductId);
                return Page();
            }

            try
            {
                await _variantService.UpdateVariantAsync(UpdateVariant);
            }
            catch (ArgumentException)
            {
                 return RedirectToPage(new { productId = UpdateVariant.ProductId });
            }

            return RedirectToPage(new { productId = UpdateVariant.ProductId });
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id, long productId)
        {
             var companyId = await _userService.GetCompanyIdFromUserAsync(User);
            if (companyId == 0)
            {
                return RedirectToPage("/Products");
            }
            
            Product = await _productService.GetByIdInCompanyAsync(productId, companyId);
            if (Product == null)
            {
                return RedirectToPage("/Products");
            }

            var variant = await _variantService.GetVariantByIdAsync(id);
            if (variant != null && variant.ProductId == productId)
            {
                await _variantService.DeleteVariantAsync(id);
            }

            return RedirectToPage(new { productId });
        }
    }
}
