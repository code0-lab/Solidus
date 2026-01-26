using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatoris.Service.Services;
using DomusMercatoris.Service.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Roles = "Manager")]
    public class BrandsModel : PageModel
    {
        private readonly BrandService _brandService;
        private readonly DomusMercatoris.Data.DomusDbContext _db;

        public BrandsModel(BrandService brandService, DomusMercatoris.Data.DomusDbContext db)
        {
            _brandService = brandService;
            _db = db;
        }

        public List<BrandDto> Brands { get; set; } = new List<BrandDto>();
        
        [BindProperty]
        public CreateBrandDto NewBrand { get; set; } = new CreateBrandDto();

        public async Task<IActionResult> OnGetAsync()
        {
            var companyId = await GetCompanyIdAsync();
            if (companyId == 0) return RedirectToPage("/Index");

            Brands = await _brandService.GetBrandsByCompanyAsync(companyId);
            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            var companyId = await GetCompanyIdAsync();
            if (companyId == 0) return RedirectToPage("/Index");

            if (!ModelState.IsValid)
            {
                Brands = await _brandService.GetBrandsByCompanyAsync(companyId);
                return Page();
            }

            NewBrand.CompanyId = companyId;
            await _brandService.CreateBrandAsync(NewBrand);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var companyId = await GetCompanyIdAsync();
            if (companyId == 0) return RedirectToPage("/Index");

            await _brandService.DeleteBrandAsync(id, companyId);
            return RedirectToPage();
        }

        private async Task<int> GetCompanyIdAsync()
        {
            var claim = User.FindFirst("CompanyId")?.Value;
            if (int.TryParse(claim, out int id)) return id;
            
            // Fallback to DB check if claim missing (simplified)
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (long.TryParse(userIdStr, out long userId))
            {
                 var user = await _db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId);
                 return user?.CompanyId ?? 0;
            }
            return 0;
        }
    }
}
