using System.Security.Claims;
using DomusMercatoris.Core.Constants;
using DomusMercatoris.Service.Interfaces;

namespace DomusMercatorisDotnetMVC.Services
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
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(AppConstants.CustomClaimTypes.UserId) 
                               ?? _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
                
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
                var companyIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(AppConstants.CustomClaimTypes.CompanyId);
                if (companyIdClaim != null && int.TryParse(companyIdClaim.Value, out var companyId))
                {
                    return companyId;
                }
                return null;
            }
        }

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        public bool IsManager => _httpContextAccessor.HttpContext?.User?.IsInRole(AppConstants.Roles.Manager) ?? false;

        public Task<bool> HasPermissionAsync(string permission)
        {
            var hasPermission = _httpContextAccessor.HttpContext?.User?.HasClaim("Permission", permission) ?? false;
            return Task.FromResult(hasPermission);
        }
    }
}
