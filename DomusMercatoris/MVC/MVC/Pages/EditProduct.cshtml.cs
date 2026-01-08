using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatorisDotnetMVC.Services;
using DomusMercatoris.Core.Entities;
using DomusMercatorisDotnetMVC.Dto.ProductDto;
using DomusMercatoris.Data;
using System.Linq;
using DomusMercatoris.Service.Services;
using DomusMercatoris.Service.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Roles = "Manager,User")]
    public class EditProductModel : PageModel
    {
        private readonly ProductService _productService;
        private readonly DomusDbContext _db;

        private readonly GeminiService _geminiService;
        private readonly UserService _userService;
        private readonly BrandService _brandService;

        public EditProductModel(ProductService productService, DomusDbContext db, GeminiService geminiService, UserService userService, BrandService brandService)
        {
            _productService = productService;
            _db = db;
            _geminiService = geminiService;
            _userService = userService;
            _brandService = brandService;
        }

        public Product? Existing { get; set; }
        public List<Category> Categories { get; set; } = new();
        public List<BrandDto> Brands { get; set; } = new();
        public AutoCategory? SuggestedAutoCategory { get; set; }

        [BindProperty]
        public ProductUpdateDto Product { get; set; } = new();

        public async Task<IActionResult> OnGet(long id)
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
            Categories = await _db.Categories.Where(c => c.CompanyId == companyId) 
            //            ^ await ile a senkron yapıldı ve veri tabanının kategori tespitinde kitlenmesi önlendi
                .OrderBy(c => c.ParentId.HasValue)
                .ThenBy(c => c.Name)
                //sadece gerekli olanları çek (.select)
                .Select(c => new Category { Id = c.Id, Name = c.Name, ParentId = c.ParentId })
                .ToListAsync();
            
            Brands = await _brandService.GetBrandsByCompanyAsync(companyId);

            Product.Name = Existing.Name;
            Product.Sku = Existing.Sku;
            Product.Description = Existing.Description;
            Product.CategoryId = Existing.CategoryId;
            Product.SubCategoryId = Existing.SubCategoryId;
            Product.BrandId = Existing.BrandId;
            Product.Price = Existing.Price;
            Product.Quantity = Existing.Quantity;
            Product.AutoCategoryId = Existing.AutoCategoryId;

            var member = await _db.ProductClusterMembers
                .Include(m => m.ProductCluster)
                .ThenInclude(c => c.AutoCategories)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            
            if (member != null && member.ProductCluster.AutoCategories.Any())
            {
                SuggestedAutoCategory = member.ProductCluster.AutoCategories.First();
            }

            var ids = new List<int>();
            ids.AddRange((Existing.Categories ?? new List<Category>()).Select(c => c.Id));
            if (Existing.CategoryId.HasValue) ids.Add(Existing.CategoryId.Value);
            if (Existing.SubCategoryId.HasValue) ids.Add(Existing.SubCategoryId.Value);
            Product.CategoryIds = ids.Distinct().ToList();
            return Page();
        }

        public async Task<IActionResult> OnPost(long id)
        {
            var comp = User.FindFirst("CompanyId")?.Value;
            var companyId = (!string.IsNullOrEmpty(comp) && int.TryParse(comp, out var c)) ? c : 0;
            Existing = _productService.GetByIdInCompany(id, companyId);
            Categories = await _db.Categories.Where(c => c.CompanyId == companyId)
                .OrderBy(c => c.ParentId.HasValue) //ParentId genelde "Üst Kategori"yi tutar. Eğer null ise o bir Ana Kategoridir. Doluysa (HasValue true ise) bir Alt Kategoridir.
                .ThenBy(c => c.Name)
                .Select(c => new Category { Id = c.Id, Name = c.Name, ParentId = c.ParentId })
                .ToListAsync();
            
            Brands = await _brandService.GetBrandsByCompanyAsync(companyId);

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
