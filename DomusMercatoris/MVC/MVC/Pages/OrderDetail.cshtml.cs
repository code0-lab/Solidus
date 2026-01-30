using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DomusMercatoris.Data;
using DomusMercatoris.Core.Entities;
using System.Linq;
using System.Threading.Tasks;
using DomusMercatorisDotnetMVC.Services;
using DomusMercatoris.Core.Constants;
using System.Security.Claims;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Policy = "OrdersAccess")]
    public class OrderDetailModel : PageModel
    {
        private readonly DomusMercatoris.Service.Services.OrderService _orderService;
        private readonly UserService _userService;

        public OrderDetailModel(DomusMercatoris.Service.Services.OrderService orderService, UserService userService)
        {
            _orderService = orderService;
            _userService = userService;
        }

        public Order? Order { get; set; }
        public CargoTracking? Tracking { get; set; }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            // 1. Get User ID (DRY & Magic String Free)
            long userId = 0;
            var userIdClaim = User.FindFirst(AppConstants.CustomClaimTypes.UserId)?.Value 
                           ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                 long.TryParse(userIdClaim, out userId);
            }

            // 2. Get Company ID (Centralized Logic)
            int companyId = await _userService.GetCompanyIdFromUserAsync(User);

            // 3. Secure Fetch (Performance: Database-level filtering)
            // Fetches order ONLY if the user has access (Own Order OR Company Order)
            var order = await _orderService.GetOrderDetailsForUserAsync(id, userId, companyId);

            if (order == null)
            {
                // Order not found OR User has no access. 
                // Returning AccessDenied as per previous logic, but strictly speaking "Not Found" is also valid for security through obscurity.
                return RedirectToPage("/Account/AccessDenied");
            }

            if (!order.IsPaid)
            {
                return RedirectToPage("/Products");
            }

            Order = order;
            Tracking = order.CargoTracking;
            
            return Page();
        }
    }
}
