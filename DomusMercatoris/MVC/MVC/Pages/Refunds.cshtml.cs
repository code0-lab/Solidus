using DomusMercatoris.Core.Models;
using DomusMercatoris.Service.DTOs;
using DomusMercatoris.Service.Services;
using DomusMercatorisDotnetMVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Policy = "RefundsAccess")]
    public class RefundsModel : PageModel
    {
        private readonly RefundService _refundService;
        private readonly UserService _userService;

        public RefundsModel(RefundService refundService, UserService userService)
        {
            _refundService = refundService;
            _userService = userService;
        }

        public List<RefundRequestDto> Refunds { get; set; } = new();

        [BindProperty]
        public UpdateRefundStatusDto UpdateStatus { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var companyId = await _userService.GetCompanyIdFromUserAsync(User);
            if (companyId <= 0)
            {
                return RedirectToPage("/Index");
            }

            Refunds = await _refundService.GetCompanyRefundsAsync(companyId);
            return Page();
        }

        public async Task<IActionResult> OnPostApproveAsync(long refundId)
        {
            var companyId = await _userService.GetCompanyIdFromUserAsync(User);
            if (companyId <= 0)
                return Unauthorized();

            var result = await _refundService.ProcessRefundRequestAsync(companyId, new UpdateRefundStatusDto
            {
                RefundRequestId = refundId,
                IsApproved = true
            });

            if (!result)
                ModelState.AddModelError("", "Failed to approve refund.");

            return await OnGetAsync();
        }

        public async Task<IActionResult> OnPostRejectAsync()
        {
             var companyId = await _userService.GetCompanyIdFromUserAsync(User);
            if (companyId <= 0)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(UpdateStatus.RejectionReason))
            {
                ModelState.AddModelError("", "Rejection reason is required.");
                return await OnGetAsync();
            }

            UpdateStatus.IsApproved = false;
            var result = await _refundService.ProcessRefundRequestAsync(companyId, UpdateStatus);

             if (!result)
                ModelState.AddModelError("", "Failed to reject refund.");

            return await OnGetAsync();
        }
    }
}
