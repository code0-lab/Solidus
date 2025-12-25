using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatorisDotnetMVC.Services;
using DomusMercatoris.Core.Entities;
using DomusMercatorisDotnetMVC.Dto.ProductDto;
using DomusMercatoris.Data;
using System.Linq;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Roles = "Manager,User")]
    public class EditProductModel : PageModel
    {
        private readonly ProductService _productService;
        private readonly DomusDbContext _db;

        private readonly GeminiService _geminiService;
        private readonly UserService _userService;

        public EditProductModel(ProductService productService, DomusDbContext db, GeminiService geminiService, UserService userService)
        {
            _productService = productService;
            _db = db;
            _geminiService = geminiService;
            _userService = userService;
        }

        public Product? Existing { get; set; }
        public List<Category> Categories { get; set; } = new();

        [BindProperty]
        public ProductUpdateDto Product { get; set; } = new();

        public IActionResult OnGet(long id)
        {
            var comp = User.FindFirst("CompanyId")?.Value;
            if (string.IsNullOrEmpty(comp) || !int.TryParse(comp, out var companyId))
            {
                return RedirectToPage("/Products");
            }
            Existing = _productService.GetByIdInCompany(id, companyId);
            if (Existing == null)
            {
                return RedirectToPage("/Products");
            }
            Categories = _db.Categories.Where(c => c.CompanyId == companyId)
                .OrderBy(c => c.ParentId.HasValue)
                .ThenBy(c => c.Name)
                .ToList();
            Product.Name = Existing.Name;
            Product.Sku = Existing.Sku;
            Product.Description = Existing.Description;
            Product.CategoryId = Existing.CategoryId;
            Product.SubCategoryId = Existing.SubCategoryId;
            Product.Price = Existing.Price;
            Product.Quantity = Existing.Quantity;
            var ids = new List<int>();
            ids.AddRange((Existing.Categories ?? new List<Category>()).Select(c => c.Id));
            if (Existing.CategoryId.HasValue) ids.Add(Existing.CategoryId.Value);
            if (Existing.SubCategoryId.HasValue) ids.Add(Existing.SubCategoryId.Value);
            Product.CategoryIds = ids.Distinct().ToList();
            return Page();
        }

        public IActionResult OnPost(long id)
        {
            var comp = User.FindFirst("CompanyId")?.Value;
            var companyId = (!string.IsNullOrEmpty(comp) && int.TryParse(comp, out var c)) ? c : 0;
            Existing = _productService.GetByIdInCompany(id, companyId);
            Categories = _db.Categories.Where(c => c.CompanyId == companyId)
                .OrderBy(c => c.ParentId.HasValue)
                .ThenBy(c => c.Name)
                .ToList();
            foreach (var kv in ModelState.Where(k => k.Key.StartsWith("Product.ImageFiles")).ToList())
            {
                kv.Value?.Errors?.Clear();
            }
            if (!ModelState.IsValid)
            {
                return Page();
            }
            var existingCount = Existing?.Images?.Count ?? 0;
            var removeCount = (Product.RemoveImages ?? new List<string>()).Count;
            var newFilesCount = (Product.ImageFiles ?? new List<Microsoft.AspNetCore.Http.IFormFile?>()).Where(f => f != null && f.Length > 0).Count();
            if (existingCount >= 4 && newFilesCount > 0 && removeCount == 0)
            {
                Product.Name = Existing?.Name ?? Product.Name;
                Product.Sku = Existing?.Sku ?? Product.Sku;
                Product.Description = Existing?.Description ?? Product.Description;
                Product.CategoryId = Existing?.CategoryId;
                Product.SubCategoryId = Existing?.SubCategoryId;
                Product.Price = Existing?.Price ?? Product.Price;

                ModelState.Remove("Product.Name");
                ModelState.Remove("Product.Sku");
                ModelState.Remove("Product.Description");
                ModelState.Remove("Product.CategoryId");
                ModelState.Remove("Product.SubCategoryId");
                ModelState.Remove("Product.Price");
                foreach (var kv in ModelState.Where(k => k.Key.StartsWith("Product.ImageFiles")).Select(k => k.Key).ToList())
                {
                    ModelState.Remove(kv);
                }
                ModelState.AddModelError(string.Empty, "Maximum 4 images allowed. Please delete at least one before adding new images.");
                return Page();
            }
            try
            {
                var updated = _productService.Update(id, Product);
                if (updated == null)
                {
                    return RedirectToPage("/Products");
                }
                TempData["Message"] = "Product updated.";
                return RedirectToPage("/ProductDetail", new { id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                Existing = _productService.GetByIdInCompany(id, companyId);
                Product.Name = Existing?.Name ?? Product.Name;
                Product.Sku = Existing?.Sku ?? Product.Sku;
                Product.Description = Existing?.Description ?? Product.Description;
                Product.CategoryId = Existing?.CategoryId;
                Product.SubCategoryId = Existing?.SubCategoryId;
                Product.Price = Existing?.Price ?? Product.Price;
                ModelState.Remove("Product.Price");
                foreach (var kv in ModelState.Where(k => k.Key.StartsWith("Product.ImageFiles")).Select(k => k.Key).ToList())
                {
                    ModelState.Remove(kv);
                }
                return Page();
            }
        }
        public async Task<IActionResult> OnPostGenerateDescriptionAsync()
        {
            try
            {
                var files = Request.Form.Files.ToList();
                if (files.Count == 0)
                {
                    return new JsonResult(new { success = false, message = "No images uploaded." });
                }

                // Check API key
                var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
                if (string.IsNullOrEmpty(apiKey))
                {
                    // Fallback to configuration if env var not set directly (optional, depends on setup)
                    // For now, assuming it's in environment or you can inject IConfiguration
                    return new JsonResult(new { success = false, message = "Gemini API Key is not configured." });
                }

                var description = await _geminiService.GenerateProductDescription(apiKey, files);
                if (string.IsNullOrEmpty(description))
                {
                    return new JsonResult(new { success = false, message = "Failed to generate description." });
                }

                return new JsonResult(new { success = true, data = description });
            }
            catch (Exception ex)
            {
                // Log error
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }
    }
}
