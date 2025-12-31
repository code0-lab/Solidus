using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
            var sale = _db.Sales.SingleOrDefault(s => s.Id == id);
            if (sale == null || !sale.IsPaid)
            {
                return RedirectToPage("/Products");
            }
            Sale = sale;
            if (sale.CargoTrackingId.HasValue)
            {
                Tracking = _db.CargoTrackings.SingleOrDefault(t => t.Id == sale.CargoTrackingId.Value);
            }
            return Page();
        }
    }
}
