using System.Security.Claims;
using DomusMercatoris.Service.Interfaces;

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

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        public bool IsManager => _httpContextAccessor.HttpContext?.User?.IsInRole("Manager") ?? false;

        public Task<bool> HasPermissionAsync(string permission)
        {
            // Simple implementation for now, checking if user has a claim with type "Permission" and value equal to the permission name
            // Or we could check roles. For now, let's assume specific permissions are claims.
            var hasPermission = _httpContextAccessor.HttpContext?.User?.HasClaim("Permission", permission) ?? false;
            return Task.FromResult(hasPermission);
        }
    }
}
