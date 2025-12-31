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
    [Authorize(Roles = "Manager,User")]
    public class ManageCargosModel : PageModel
    {
        private readonly CargoService _cargoService;
        private readonly UserService _userService;

        public ManageCargosModel(CargoService cargoService, UserService userService)
        {
            _cargoService = cargoService;
            _userService = userService;
        }

        public List<CargoTrackingDto> Cargos { get; set; } = new List<CargoTrackingDto>();

        [BindProperty]
        public CreateCargoTrackingDto NewCargo { get; set; } = new CreateCargoTrackingDto();

        [BindProperty]
        public string? UserEmail { get; set; } // To find user by email

        [BindProperty]
        public UpdateCargoStatusDto UpdateStatus { get; set; } = new UpdateCargoStatusDto();

        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TrackingNumber { get; set; }

        public CargoTrackingDto? SearchResult { get; set; }
        public List<CargoTrackingDto> MyCargos { get; set; } = new List<CargoTrackingDto>();

        public async Task OnGetAsync()
        {
            // 1. Search Logic
            if (!string.IsNullOrEmpty(TrackingNumber))
            {
                SearchResult = await _cargoService.GetByTrackingNumberAsync(TrackingNumber);
                if (SearchResult == null)
                {
                    ErrorMessage = "No cargo found with this tracking number.";
                }
            }

            // 2. Load List based on Role
            if (User.IsInRole("Manager"))
            {
                // Managers see all cargos
                Cargos = await _cargoService.GetAllCargosAsync();
            }
            else
            {
                // Regular users see only their cargos
                var userEmail = User.Identity?.Name;
                if (!string.IsNullOrEmpty(userEmail))
                {
                    var user = await _userService.GetUserByEmailAsync(userEmail);
                    if (user != null)
                    {
                        MyCargos = await _cargoService.GetUserCargosAsync(user.Id);
                    }
                }
            }
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Please check the form inputs.";
                Cargos = await _cargoService.GetAllCargosAsync();
                return Page();
            }

            if (!string.IsNullOrEmpty(UserEmail))
            {
                var user = await _userService.GetUserByEmailAsync(UserEmail);
                if (user != null)
                {
                    NewCargo.UserId = user.Id;
                }
                else
                {
                    ErrorMessage = "User not found with this email.";
                    Cargos = await _cargoService.GetAllCargosAsync();
                    return Page();
                }
            }
            else
            {
                // If no email provided, try to assign to current user
                var currentUserEmail = User.Identity?.Name;
                if (!string.IsNullOrEmpty(currentUserEmail))
                {
                    var user = await _userService.GetUserByEmailAsync(currentUserEmail);
                    if (user != null) NewCargo.UserId = user.Id;
                }
            }

            await _cargoService.CreateTrackingAsync(NewCargo);
            Message = "Cargo tracking created successfully.";
            return RedirectToPage();
        }

            public async Task<IActionResult> OnPostUpdateStatusAsync()
        {

            var result = await _cargoService.UpdateStatusAsync(UpdateStatus);
            if (result)
            {
                Message = "Status updated successfully.";
            }
            else
            {
                ErrorMessage = "Failed to update status. Cargo not found.";
            }
            return RedirectToPage();
        }
    }
}