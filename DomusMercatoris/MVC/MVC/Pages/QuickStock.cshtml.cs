using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Roles = "Manager,User,Rex")]
    public class QuickStockModel : PageModel
    {
        private readonly DomusDbContext _db;
        private readonly DomusMercatorisDotnetMVC.Services.UserService _userService;

        public QuickStockModel(DomusDbContext db, DomusMercatorisDotnetMVC.Services.UserService userService)
        {
            _db = db;
            _userService = userService;
        }

        [BindProperty(SupportsGet = true)]
        public long Id { get; set; }

        public Product? Product { get; set; }

        [BindProperty]
        public int Quantity { get; set; }

        [BindProperty]
        public string? ShelfNumber { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (Id <= 0) return RedirectToPage("/Dashboard");

            var companyId = await _userService.GetCompanyIdFromUserAsync(User);

            Product = await _db.Products
                .FirstOrDefaultAsync(p => p.Id == Id && p.CompanyId == companyId);

            if (Product == null) return Page();

            Quantity = Product.Quantity;
            ShelfNumber = Product.ShelfNumber;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var companyId = await _userService.GetCompanyIdFromUserAsync(User);
            
            var product = await _db.Products
                .FirstOrDefaultAsync(p => p.Id == Id && p.CompanyId == companyId);

            if (product == null) return NotFound();

            product.Quantity = Quantity;
            product.ShelfNumber = ShelfNumber;

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Stock updated successfully!";
            return RedirectToPage("/Dashboard");
        }
    }
}
