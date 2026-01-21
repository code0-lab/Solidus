using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DomusMercatorisDotnetMVC.Dto.CommentsDto;
using DomusMercatorisDotnetMVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Roles = "Manager,user")]
    public class CommentsModel : PageModel
    {
        private readonly CommentService _commentService;
        private readonly UserService _userService;

        public CommentsModel(CommentService commentService, UserService userService)
        {
            _commentService = commentService;
            _userService = userService;
        }

        public int CompanyId { get; set; }
        public List<ProductCommentsSummaryDto> Products { get; set; } = new();
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; } = 1;

        public async Task<IActionResult> OnGetAsync()
        {
            var pageStr = Request.Query["page"].ToString();
            if (!string.IsNullOrEmpty(pageStr) && int.TryParse(pageStr, out var p))
            {
                PageNumber = Math.Max(1, p);
            }

            var comp = User.FindFirst("CompanyId")?.Value;
            if (!string.IsNullOrEmpty(comp) && int.TryParse(comp, out var companyId))
            {
                CompanyId = companyId;
            }
            else
            {
                var idClaim = User.FindFirst("UserId")?.Value;
                if (!string.IsNullOrEmpty(idClaim) && long.TryParse(idClaim, out var userId))
                {
                    var me = await _userService.GetByIdAsync(userId);
                    if (me != null)
                    {
                        CompanyId = me.CompanyId;
                    }
                }
            }

            if (CompanyId <= 0)
            {
                Products = new List<ProductCommentsSummaryDto>();
                return Page();
            }

            var result = await _commentService.GetProductsWithCommentsForCompanyAsync(CompanyId, PageNumber, PageSize);
            TotalCount = result.TotalCount;
            TotalPages = Math.Max(1, (int)System.Math.Ceiling(TotalCount / (double)PageSize));
            if (PageNumber > TotalPages) PageNumber = TotalPages;
            Products = result.Items;
            return Page();
        }
    }
}
