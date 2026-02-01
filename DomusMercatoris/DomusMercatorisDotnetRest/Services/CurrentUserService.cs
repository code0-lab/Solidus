using System.Security.Claims;
using DomusMercatoris.Service.Interfaces;
using DomusMercatoris.Core.Constants;

namespace DomusMercatorisDotnetRest.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public long? UserId
        {
            get
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
                {
                    return userId;
                }
                return null;
            }
        }

        public int? CompanyId
        {
            get
            {
                var companyIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("CompanyId");
                if (companyIdClaim != null && int.TryParse(companyIdClaim.Value, out var companyId))
                {
                    return companyId;
                }
                return null;
            }
        }

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        public bool IsManager => _httpContextAccessor.HttpContext?.User?.IsInRole(AppConstants.Roles.Manager) ?? false;

        public bool IsInRole(string role) => _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;

        public Task<bool> HasPermissionAsync(string permission)
        {
            // Simple implementation for now, checking if user has a claim with type "Permission" and value equal to the permission name
            // Or we could check roles. For now, let's assume specific permissions are claims.
            var hasPermission = _httpContextAccessor.HttpContext?.User?.HasClaim("Permission", permission) ?? false;
            return Task.FromResult(hasPermission);
        }
    }
}
