using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DomusMercatorisDotnetMVC.Middleware
{
    public class BanEnforcementMiddleware
    {
        private readonly RequestDelegate _next;

        public BanEnforcementMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity != null && 
                context.User.Identity.IsAuthenticated && 
                context.User.IsInRole("Banned"))
            {
                var path = context.Request.Path.Value?.ToLower() ?? string.Empty;

                // 1. Static Files Bypass (CSS, JS, Images, Fonts, etc.)
                // Ban check shouldn't block static assets
                if (path.StartsWith("/css") || 
                    path.StartsWith("/js") || 
                    path.StartsWith("/lib") || 
                    path.StartsWith("/img") ||
                    path.StartsWith("/uploads") ||
                    path.Contains("favicon.ico"))
                {
                    await _next(context);
                    return;
                }

                // 2. Allow access to Baned page
                if (path.StartsWith("/banned"))
                {
                    await _next(context);
                    return;
                }

                // 3. Allow logout
                // Flexible Logout Check: Any path containing "Logout" in query string or path segment
                // Covers: /Index?handler=Logout, /?handler=Logout, /Account/Logout, etc.
                if (path.Contains("logout") || 
                   (context.Request.Query.ContainsKey("handler") && context.Request.Query["handler"].ToString().ToLower() == "logout"))
                {
                    await _next(context);
                    return;
                }

                // Redirect everything else to Baned page
                context.Response.Redirect("/Banned");
                return;
            }

            await _next(context);
        }
    }
}
