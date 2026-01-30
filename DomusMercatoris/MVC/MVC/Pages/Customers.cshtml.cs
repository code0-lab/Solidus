using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatorisDotnetMVC.Services;
using DomusMercatoris.Core.Entities;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using DomusMercatoris.Service.DTOs;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Policy = "CustomersAccess")]
    public class CustomersModel : PageModel
    {
        private readonly UserService _userService;
        private readonly IMapper _mapper;

        public CustomersModel(UserService userService, IMapper mapper)
        {
            _userService = userService;
            _mapper = mapper;
        }

        public List<UserDto> Customers { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var companyId = await _userService.GetCompanyIdFromUserAsync(User);
            if (companyId <= 0)
            {
                return RedirectToPage("/Index");
            }

            Customers = await _userService.GetCustomersByCompanyAsync(companyId);
            return Page();
        }
    }
}
