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
    [Authorize(Roles = "Manager,User")]
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
        
        public List<CommentsDto> Comments { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? Status { get; set; } // null: All, 0: Pending, 1: Approved, 2: Rejected

        [BindProperty(SupportsGet = true, Name = "page")]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; } = 1;

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadCompanyIdAsync();

            if (CompanyId <= 0)
            {
                Comments = new List<CommentsDto>();
                return Page();
            }

            var result = await _commentService.GetCommentsForCompanyAsync(CompanyId, Status, PageNumber, PageSize);
            TotalCount = result.TotalCount;
            TotalPages = Math.Max(1, (int)System.Math.Ceiling(TotalCount / (double)PageSize));
            
            if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;
            
            Comments = result.Items;
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int commentId, bool isApproved)
        {
            await LoadCompanyIdAsync();
            if (CompanyId <= 0) return Forbid();

            var success = await _commentService.SetApprovalAsync(commentId, isApproved, CompanyId);
            if (!success)
            {
                TempData["Error"] = "Comment not found or access denied.";
            }
            else
            {
                TempData["Success"] = $"Comment {(isApproved ? "approved" : "rejected")} successfully.";
            }

            return RedirectToPage("/Comments", new { page = PageNumber, status = Status });
        }

        private async Task LoadCompanyIdAsync()
        {
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
        }
    }
}
