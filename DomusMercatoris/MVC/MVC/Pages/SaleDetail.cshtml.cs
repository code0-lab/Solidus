using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DomusMercatoris.Data;
using DomusMercatoris.Core.Entities;
using System.Linq;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Roles = "Manager,User")]
    public class SaleDetailModel : PageModel
    {
        private readonly DomusDbContext _db;

        public SaleDetailModel(DomusDbContext db)
        {
            _db = db;
        }

        public Sale? Sale { get; set; }
        public CargoTracking? Tracking { get; set; }

        public IActionResult OnGet(long id)
        {
            var sale = _db.Sales
                .Include(s => s.User)
                .Include(s => s.FleetingUser)
                .Include(s => s.SaleProducts)
                    .ThenInclude(sp => sp.Product)
                .Include(s => s.SaleProducts)
                    .ThenInclude(sp => sp.VariantProduct)
                .Include(s => s.CargoTracking)
                .SingleOrDefault(s => s.Id == id);

            if (sale == null || !sale.IsPaid)
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
                    var me = _db.Users.SingleOrDefault(u => u.Id == userId);
                    if (me != null) companyId = me.CompanyId;
                }
            }

            var currentUserIdClaim = User.FindFirst("UserId")?.Value;
            long currentUserId = 0;
            if (!string.IsNullOrEmpty(currentUserIdClaim)) long.TryParse(currentUserIdClaim, out currentUserId);

            // Access Check
            bool hasAccess = false;

            // 1. Is it the user's own purchase?
            if (sale.UserId == currentUserId && currentUserId > 0)
            {
                hasAccess = true;
            }
            // 2. Is it a sale for the user's company?
            else if (companyId > 0 && sale.CompanyId == companyId)
            {
                hasAccess = true;
            }

            if (!hasAccess)
            {
                return RedirectToPage("/Account/AccessDenied");
            }

            Sale = sale;
            Tracking = sale.CargoTracking;
            
            return Page();
        }
    }
}
