using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatoris.Service.Services;
using DomusMercatoris.Service.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using DomusMercatorisDotnetMVC.Services;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Policy = "BrandsAccess")]
    public class BrandsModel : PageModel
    {
        private readonly BrandService _brandService;
        private readonly UserService _userService;

        public BrandsModel(BrandService brandService, UserService userService)
        {
            _brandService = brandService;
            _userService = userService;
        }

        public List<BrandDto> Brands { get; set; } = new List<BrandDto>();
        
        [BindProperty]
        public CreateBrandDto NewBrand { get; set; } = new CreateBrandDto();

        [BindProperty]
        public UpdateBrandDto UpdateBrand { get; set; } = new UpdateBrandDto();

        public async Task<IActionResult> OnGetAsync()
        {
            var companyId = await _userService.GetCompanyIdFromUserAsync(User);
            if (companyId == 0) return RedirectToPage("/Index");

            Brands = await _brandService.GetBrandsByCompanyAsync(companyId);
            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            var companyId = await _userService.GetCompanyIdFromUserAsync(User);
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

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            var companyId = await _userService.GetCompanyIdFromUserAsync(User);
            if (companyId == 0) return RedirectToPage("/Index");
            
            // Simple approach: Check UpdateBrand properties manually or ignore NewBrand errors.
            ModelState.ClearValidationState(nameof(NewBrand));
            if (!TryValidateModel(UpdateBrand, nameof(UpdateBrand)))
            {
                Brands = await _brandService.GetBrandsByCompanyAsync(companyId);
                return Page();
            }

            await _brandService.UpdateBrandAsync(UpdateBrand, companyId);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var companyId = await _userService.GetCompanyIdFromUserAsync(User);
            if (companyId == 0) return RedirectToPage("/Index");

            await _brandService.DeleteBrandAsync(id, companyId);
            return RedirectToPage();
        }
    }
}
