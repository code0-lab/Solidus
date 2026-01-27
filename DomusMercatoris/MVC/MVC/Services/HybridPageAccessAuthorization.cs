using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace DomusMercatorisDotnetMVC.Services
{
    public class HybridPageAccessRequirement : IAuthorizationRequirement
    {
        public string PageKey { get; }
        public string[] RequiredRoles { get; }

        public HybridPageAccessRequirement(string pageKey, params string[] requiredRoles)
        {
            PageKey = pageKey;
            RequiredRoles = requiredRoles ?? Array.Empty<string>();
        }
    }

    public class HybridPageAccessHandler : AuthorizationHandler<HybridPageAccessRequirement>
    {
        private readonly DomusDbContext _db;

        public HybridPageAccessHandler(DomusDbContext db)
        {
            _db = db;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, HybridPageAccessRequirement requirement)
        {
            var roles = context.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
            if (roles.Any(r => requirement.RequiredRoles.Contains(r, StringComparer.OrdinalIgnoreCase)))
            {
                context.Succeed(requirement);
                return;
            }

            var userIdStr = context.User.FindFirst("UserId")?.Value;
            var companyIdStr = context.User.FindFirst("CompanyId")?.Value;

            if (long.TryParse(userIdStr, out var userId) && int.TryParse(companyIdStr, out var companyId))
            {
                var hasAccess = await _db.UserPageAccesses
                    .AsNoTracking()
                    .AnyAsync(a => a.UserId == userId && a.CompanyId == companyId && a.PageKey == requirement.PageKey);

                if (hasAccess)
                {
                    context.Succeed(requirement);
                }
            }
        }
    }
}

