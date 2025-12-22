using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatorisDotnetMVC.Dto.ProductDto;
using DomusMercatorisDotnetMVC.Services;
using DomusMercatorisDotnetMVC.Utils;
using DomusMercatorisDotnetMVC.Models;
using System.Linq;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Roles = "Manager,User")]
    public class AddProductModel : PageModel
    {
        [BindProperty]
        public ProductCreateDto Product { get; set; } = new();

        private readonly ProductService _productService;
        private readonly ApplicationDbContext _db;
        private readonly GeminiService _geminiService;
        private readonly UserService _userService;

        public List<Category> Categories { get; set; } = new();
        public AddProductModel(ProductService productService, ApplicationDbContext db, GeminiService geminiService, UserService userService)
        {
            _productService = productService;
            _db = db;
            _geminiService = geminiService;
            _userService = userService;
        }

        public async Task<IActionResult> OnPostGenerateDescription(List<IFormFile> files)
        {
            var comp = User.FindFirst("CompanyId")?.Value;
            int companyId = 0;
            if (!string.IsNullOrEmpty(comp) && int.TryParse(comp, out var cid))
            {
                companyId = cid;
            }
            else
            {
                var idClaim = User.FindFirst("UserId")?.Value;
                if (!string.IsNullOrEmpty(idClaim) && long.TryParse(idClaim, out var userId))
                {
                    var me = _db.Users.SingleOrDefault(u => u.Id == userId);
                    if (me != null) companyId = me.CompanyId;
                }
            }

            if (companyId == 0) return new JsonResult(new { success = false, message = "Company not found" });

            var apiKey = _userService.GetCompanyGeminiKey(companyId);
            if (string.IsNullOrEmpty(apiKey))
            {
                return new JsonResult(new { success = false, message = "Gemini API Key is missing. Please configure it in company settings." });
            }

            var result = await _geminiService.GenerateProductDescription(apiKey, files);
            if (string.IsNullOrEmpty(result))
            {
                return new JsonResult(new { success = false, message = "Failed to generate description." });
            }

            return new JsonResult(new { success = true, data = result });
        }

        public void OnGet()
        {
            var comp = User.FindFirst("CompanyId")?.Value;
            int companyId = 0;
            if (!string.IsNullOrEmpty(comp) && int.TryParse(comp, out var cid))
            {
                companyId = cid;
            }
            else
            {
                var idClaim = User.FindFirst("UserId")?.Value;
                if (!string.IsNullOrEmpty(idClaim) && long.TryParse(idClaim, out var userId))
                {
                    var me = _db.Users.SingleOrDefault(u => u.Id == userId);
                    if (me != null) companyId = me.CompanyId;
                }
            }
            if (companyId > 0)
            {
                Categories = _db.Categories.Where(c => c.CompanyId == companyId)
                    .OrderBy(c => c.ParentId.HasValue)
                    .ThenBy(c => c.Name)
                    .ToList();
            }
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                OnGet();
                return Page();
            }
            _productService.Create(Product);
            TempData["Message"] = "Product added.";
            return RedirectToPage("/Dashboard");
        }
    }
}
