using Microsoft.AspNetCore.Mvc;
using DomusMercatoris.Service.Services;
using DomusMercatoris.Service.Interfaces;
using DomusMercatoris.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace DomusMercatorisDotnetRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BlacklistController : ControllerBase
    {
        private readonly BlacklistService _blacklistService;
        private readonly ICurrentUserService _currentUserService;

        public BlacklistController(BlacklistService blacklistService, ICurrentUserService currentUserService)
        {
            _blacklistService = blacklistService;
            _currentUserService = currentUserService;
        }

        [HttpGet("status/{companyId}")]
        public async Task<ActionResult<BlacklistStatus>> GetStatus(int companyId)
        {
            var userId = _currentUserService.UserId;
            if (userId == null) return Unauthorized();

            var status = await _blacklistService.GetStatusAsync(companyId, userId.Value);
            return Ok(status);
        }

        [HttpPost("block-company/{companyId}")]
        public async Task<IActionResult> BlockCompany(int companyId)
        {
            var userId = _currentUserService.UserId;
            if (userId == null) return Unauthorized();

            await _blacklistService.BlockByCustomerAsync(userId.Value, companyId);
            return Ok(new { message = "Company blocked successfully" });
        }

        [HttpPost("unblock-company/{companyId}")]
        public async Task<IActionResult> UnblockCompany(int companyId)
        {
            var userId = _currentUserService.UserId;
            if (userId == null) return Unauthorized();

            await _blacklistService.UnblockByCustomerAsync(userId.Value, companyId);
            return Ok(new { message = "Company unblocked successfully" });
        }

        // Manager endpoints (Company blocking Customer)
        [HttpPost("block-customer/{customerId}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> BlockCustomer(long customerId)
        {
            var companyId = _currentUserService.CompanyId;
            if (companyId == null) return Unauthorized("Company context required");

            await _blacklistService.BlockByCompanyAsync(companyId.Value, customerId);
            return Ok(new { message = "Customer blocked successfully" });
        }

        [HttpPost("unblock-customer/{customerId}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UnblockCustomer(long customerId)
        {
            var companyId = _currentUserService.CompanyId;
            if (companyId == null) return Unauthorized("Company context required");

            await _blacklistService.UnblockByCompanyAsync(companyId.Value, customerId);
            return Ok(new { message = "Customer unblocked successfully" });
        }
        
        [HttpGet("my-blocked-companies")]
        public async Task<ActionResult<List<int>>> GetMyBlockedCompanies()
        {
            var userId = _currentUserService.UserId;
            if (userId == null) return Unauthorized();

            var list = await _blacklistService.GetCompaniesBlockedByCustomerAsync(userId.Value);
            return Ok(list);
        }

        [HttpGet("company-blocked-customers")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult<List<long>>> GetCompanyBlockedCustomers()
        {
            var companyId = _currentUserService.CompanyId;
            if (companyId == null) return Unauthorized("Company context required");

            var list = await _blacklistService.GetCustomersBlockedByCompanyAsync(companyId.Value);
            return Ok(list);
        }
    }
}
