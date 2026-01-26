using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DomusMercatoris.Data;
using DomusMercatoris.Core.Entities;
using System.Linq;
using System.Threading.Tasks;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Roles = "Manager,User")]
    public class OrderDetailModel : PageModel
    {
        private readonly DomusDbContext _db;

        public OrderDetailModel(DomusDbContext db)
        {
            _db = db;
        }

        public Order? Order { get; set; }
        public CargoTracking? Tracking { get; set; }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            var order = await _db.Orders
                .AsNoTracking()
                .Include(s => s.User)
                .Include(s => s.FleetingUser)
                .Include(s => s.OrderItems)
                    .ThenInclude(sp => sp.Product)
                .Include(s => s.OrderItems)
                    .ThenInclude(sp => sp.VariantProduct)
                .Include(s => s.CargoTracking)
                .SingleOrDefaultAsync(s => s.Id == id);

            if (order == null || !order.IsPaid)
            {
                return RedirectToPage("/Products");
            }

            // Determine current user context
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
                    var me = await _db.Users.SingleOrDefaultAsync(u => u.Id == userId);
                    if (me != null) companyId = me.CompanyId;
                }
            }

            var currentUserIdClaim = User.FindFirst("UserId")?.Value;
            long currentUserId = 0;
            if (!string.IsNullOrEmpty(currentUserIdClaim)) long.TryParse(currentUserIdClaim, out currentUserId);

            // Access Check
            bool hasAccess = false;

            // 1. Is it the user's own purchase?
            if (order.UserId == currentUserId && currentUserId > 0)
            {
                hasAccess = true;
            }
            // 2. Is it a sale for the user's company?
            else if (companyId > 0 && order.CompanyId == companyId)
            {
                hasAccess = true;
            }

            if (!hasAccess)
            {
                return RedirectToPage("/Account/AccessDenied");
            }

            Order = order;
            Tracking = order.CargoTracking;
            
            return Page();
        }
    }
}
