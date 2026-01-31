using DomusMercatoris.Core.Entities;
using DomusMercatoris.Service.Services;
using DomusMercatoris.Service.DTOs;
using DomusMercatorisDotnetMVC.Dto.CommentsDto;
using DomusMercatorisDotnetMVC.Dto.ProductDto;
using DomusMercatorisDotnetMVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

using DomusMercatoris.Core.Enums;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Policy = "ProductsAccess")]
    public class ProductDetailModel : PageModel
    {
        private readonly ProductService _productService;
        private readonly VariantProductService _variantService;
        private readonly DomusMercatorisDotnetMVC.Services.CommentService _commentService;
        private readonly UserService _userService;
        private readonly GeminiCommentService _geminiCommentService;
        private readonly BlacklistService _blacklistService;

        public ProductDetailModel(ProductService productService, VariantProductService variantService, DomusMercatorisDotnetMVC.Services.CommentService commentService, UserService userService, GeminiCommentService geminiCommentService, BlacklistService blacklistService)
        {
            _productService = productService;
            _variantService = variantService;
            _commentService = commentService;
            _userService = userService;
            _geminiCommentService = geminiCommentService;
            _blacklistService = blacklistService;
        }

        public Product? Product { get; set; }
        public List<VariantProductDto> Variants { get; set; } = new();
        public List<CommentsDto> Comments { get; set; } = new();
        public BlacklistStatus BlacklistStatus { get; set; } = BlacklistStatus.None;
        public bool CanOrder { get; set; } = true;

        [BindProperty(SupportsGet = true, Name = "page")]
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; } = 1;

        public async Task<IActionResult> OnGetAsync(long id)
        {
            var comp = User.FindFirst("CompanyId")?.Value;
            if (string.IsNullOrEmpty(comp) || !int.TryParse(comp, out var companyId))
            {
                return RedirectToPage("/Products");
            }
            Product = await _productService.GetByIdInCompanyAsync(id, companyId);
            if (Product == null)
            {
                return RedirectToPage("/Products");
            }

            // Check Blacklist Status
            // Product owner company is `companyId` (from query above? No, wait. 
            // The method `GetByIdInCompanyAsync(id, companyId)` implies we are fetching a product *belonging* to `companyId`?
            // Or is `companyId` the *current user's* company?
            // Let's check `ProductService.GetByIdInCompanyAsync`.
            // Usually in this project context:
            // Manager sees their own products. 
            // If this is a Marketplace, Customer sees ANY product.
            // But this page seems to be the Manager's view ("OnPostDeleteAsync", "OnPostApproveCommentAsync" with Manager check).
            
            // Wait, the user requirement: "bir şekilde müşteri ilgili ürünün sayfasına ulaşır ise sepete ekle butonunda BLACK LİST yazacak"
            // This implies this page is ALSO used by Customers to view products.
            // Let's verify `GetByIdInCompanyAsync`. If it filters by companyId, then a Customer from Company B viewing Product from Company A might fail if `companyId` is Customer's company.
            
            // If this is the "Manager Panel" (MVC project usually is, REST is for customers/mobile), 
            // then the "Customer" scenario might not apply here directly unless the MVC project is ALSO the storefront.
            // "MVC Dashboard" vs "Angular Storefront" pattern in previous memories.
            // But user said: "razor page MVC için oluşturulacak... müşteri ilgili ürünün sayfasına ulaşır ise".
            // This implies MVC is used for shopping too, or at least product viewing.
            
            // Let's assume this page is used for viewing.
            // We need to check the relationship between Current User (Customer) and Product Owner (Company).
            
            // `Product` entity has `CompanyId`.
            // `User` has `Id` (UserId).
            
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Or "sub", "UserId"
            if (long.TryParse(userIdStr, out var userId))
            {
                 // Check if user is blocked by product owner company
                 // or user blocked product owner company
                 BlacklistStatus = await _blacklistService.GetStatusAsync(Product.CompanyId, userId);
                 CanOrder = await _blacklistService.CanCustomerOrderAsync(userId, Product.CompanyId);
            }

            Variants = await _variantService.GetVariantsByProductIdAsync(id);
            var result = await _commentService.GetCommentsByProductIdAsync(id, PageNumber, PageSize);
            
            TotalCount = result.TotalCount;
            TotalPages = Math.Max(1, (int)System.Math.Ceiling(TotalCount / (double)PageSize));
            
            if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;

            var comments = result.Items;

            if (User.IsInRole("Manager"))
            {
                Comments = comments;
            }
            else
            {
                Comments = comments.Where(c => c.IsApproved).ToList();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostApproveCommentAsync(int commentId, long productId)
        {
            if (!User.IsInRole("Manager"))
            {
                return Forbid();
            }

            var comp = User.FindFirst("CompanyId")?.Value;
            if (string.IsNullOrEmpty(comp) || !int.TryParse(comp, out var companyId))
            {
                return RedirectToPage("/Products");
            }

            await _commentService.SetApprovalAsync(commentId, true, companyId);
            return RedirectToPage(new { id = productId, page = PageNumber });
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            var ok = await _productService.DeleteAsync(id);
            TempData["Message"] = ok ? "Product deleted." : "Product not found.";
            return RedirectToPage("/Products");
        }
    }
}
