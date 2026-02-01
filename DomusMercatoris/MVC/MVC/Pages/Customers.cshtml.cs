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
        private readonly DomusMercatoris.Service.Services.BlacklistService _blacklistService;

        public CustomersModel(UserService userService, IMapper mapper, DomusMercatoris.Service.Services.BlacklistService blacklistService)
        {
            _userService = userService;
            _mapper = mapper;
            _blacklistService = blacklistService;
        }

        public List<UserDto> Customers { get; set; } = new();
        public HashSet<long> BlockedCustomerIds { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var companyId = await _userService.GetCompanyIdFromUserAsync(User);
            if (companyId <= 0)
            {
                return RedirectToPage("/Index");
            }

            Customers = await _userService.GetCustomersByCompanyAsync(companyId);
            var blockedList = await _blacklistService.GetCustomersBlockedByCompanyAsync(companyId);
            BlockedCustomerIds = new HashSet<long>(blockedList);

            return Page();
        }

        public async Task<IActionResult> OnPostBlockAsync(long customerId)
        {
            var companyId = await _userService.GetCompanyIdFromUserAsync(User);
            if (companyId <= 0) return RedirectToPage("/Index");

            await _blacklistService.BlockByCompanyAsync(companyId, customerId);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUnblockAsync(long customerId)
        {
            var companyId = await _userService.GetCompanyIdFromUserAsync(User);
            if (companyId <= 0) return RedirectToPage("/Index");

            await _blacklistService.UnblockByCompanyAsync(companyId, customerId);
            return RedirectToPage();
        }
    }
}
