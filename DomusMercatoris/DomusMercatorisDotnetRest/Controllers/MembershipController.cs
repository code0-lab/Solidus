using DomusMercatoris.Service.DTOs;
using DomusMercatoris.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DomusMercatorisDotnetRest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MembershipController : ControllerBase
    {
        private readonly MembershipService _membershipService;

        public MembershipController(MembershipService membershipService)
        {
            _membershipService = membershipService;
        }

        /// <summary>
        /// Gets the list of companies the current user is a member of.
        /// </summary>
        /// <returns>List of membership details</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<MembershipDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetMyMemberships()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var result = await _membershipService.GetUserMembershipsAsync(userId);
            return Ok(result);
        }

        /// <summary>
        /// Joins the current user to a specific company.
        /// </summary>
        /// <param name="companyId">The ID of the company to join</param>
        /// <returns>Success status</returns>
        [HttpPost("{companyId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> JoinCompany(int companyId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var success = await _membershipService.JoinCompanyAsync(userId, companyId);
            if (!success)
                return BadRequest("Could not join company. It might not exist or be inactive.");

            return Ok();
        }

        /// <summary>
        /// Removes the current user's membership from a specific company.
        /// </summary>
        /// <param name="companyId">The ID of the company to leave</param>
        /// <returns>Success status</returns>
        [HttpDelete("{companyId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> LeaveCompany(int companyId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var success = await _membershipService.LeaveCompanyAsync(userId, companyId);
            if (!success)
                return NotFound("Membership not found.");

            return Ok();
        }

        /// <summary>
        /// Searches for companies by name. Returns summary including membership status.
        /// </summary>
        /// <param name="query">The search query string</param>
        /// <returns>List of companies matching the query</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(List<CompanySummaryDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var result = await _membershipService.SearchCompaniesAsync(userId, query);
            return Ok(result);
        }
    }
}
