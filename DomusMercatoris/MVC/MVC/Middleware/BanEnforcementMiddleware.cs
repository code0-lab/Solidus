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
                context.User.IsInRole("Baned"))
            {
                var path = context.Request.Path.Value?.ToLower() ?? string.Empty;

                // Allow access to Baned page
                if (path.StartsWith("/baned"))
                {
                    await _next(context);
                    return;
                }

                // Allow logout
                // Logout is usually /Index?handler=Logout or just /?handler=Logout
                if ((path == "/index" || path == "/") && context.Request.Query.ContainsKey("handler") && context.Request.Query["handler"] == "Logout")
                {
                    await _next(context);
                    return;
                }

                // Redirect everything else to Baned page
                if (!path.StartsWith("/baned"))
                {
                     context.Response.Redirect("/Baned");
                     return;
                }
            }

            await _next(context);
        }
    }
}
