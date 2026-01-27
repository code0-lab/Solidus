using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatorisDotnetMVC.Dto.ProductDto;
using DomusMercatorisDotnetMVC.Services;
using DomusMercatoris.Data;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Service.Services;
using DomusMercatoris.Service.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Roles = "Manager,User")]
    public class AddProductModel : PageModel
    {
        [BindProperty]
        public ProductCreateDto Product { get; set; } = new();

        private readonly ProductService _productService;
        private readonly GeminiService _geminiService;
        private readonly UserService _userService;
        private readonly BrandService _brandService;
        private readonly DomusMercatorisDotnetMVC.Services.IClusteringService _clusteringService;

        public List<Category> Categories { get; set; } = new();
        public List<BrandDto> Brands { get; set; } = new();

        public AddProductModel(ProductService productService, GeminiService geminiService, UserService userService, BrandService brandService, DomusMercatorisDotnetMVC.Services.IClusteringService clusteringService)
        {
            _productService = productService;
            _geminiService = geminiService;
            _userService = userService;
            _brandService = brandService;
            _clusteringService = clusteringService;
        }
        //aşağıdaki task yapısında await ile a senkron yapıldı ve veri tabanının kategori tespitinde kitlenmesi önlendi
        public async Task<IActionResult> OnPostGenerateDescription(List<IFormFile> files)
        {
            var companyId = await _userService.GetCompanyIdFromUserAsync(User);

            if (companyId == 0) return new JsonResult(new { success = false, message = "Company not found" });

            var apiKey = await _geminiService.GetCompanyGeminiKeyAsync(companyId);
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

        public async Task OnGet()
        {
            var companyId = await _userService.GetCompanyIdFromUserAsync(User);
            if (companyId > 0)
            {
                Categories = await _productService.GetCategoriesByCompanyAsync(companyId);
                
                Brands = await _brandService.GetBrandsByCompanyAsync(companyId);
            }
        }

        public async Task<IActionResult> OnPost(string action)
        {
            if (!ModelState.IsValid)
            {
                await OnGet();
                return Page();
            }

            var product = await _productService.CreateAsync(Product);
            
            if (action == "save-add-variants")
            {
                return RedirectToPage("/ManageVariants", new { productId = product.Id });
            }

            return RedirectToPage("/Products");
        }

        public async Task<IActionResult> OnPostAutoDetectAsync()
        {
            var files = Request.Form.Files.ToList();
            if (files == null || files.Count == 0) return new JsonResult(new { success = false, message = "No files uploaded" });

            var vector = await _clusteringService.ExtractFeaturesFromFilesAsync(files);
            if (vector == null) return new JsonResult(new { success = false, message = "Could not extract features" });

            var cluster = await _clusteringService.FindNearestClusterAsync(vector);
            if (cluster == null) return new JsonResult(new { success = false, message = "No matching cluster found" });
            
            // Get AutoCategory (first one)
            var autoCategory = cluster.AutoCategories.FirstOrDefault();
            if (autoCategory == null) return new JsonResult(new { success = false, message = "Cluster found but no Auto Category assigned" });

            return new JsonResult(new { 
                success = true, 
                autoCategoryId = autoCategory.Id,
                name = autoCategory.Name,
                description = autoCategory.Description
            });
        }
    }
}
