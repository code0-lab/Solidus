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

        public ProductDetailModel(ProductService productService, VariantProductService variantService, DomusMercatorisDotnetMVC.Services.CommentService commentService, UserService userService, GeminiCommentService geminiCommentService)
        {
            _productService = productService;
            _variantService = variantService;
            _commentService = commentService;
            _userService = userService;
            _geminiCommentService = geminiCommentService;
        }

        public Product? Product { get; set; }
        public List<VariantProductDto> Variants { get; set; } = new();
        public List<CommentsDto> Comments { get; set; } = new();

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
