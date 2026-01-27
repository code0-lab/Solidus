using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;

namespace MVC.Tests;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public static string DefaultScheme = "TestScheme";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IHttpContextAccessor httpContextAccessor) : base(options, logger, encoder)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if we have set specific claims for this request in specific header or item
        // But for simplicity, we can inspect a static/scoped configuration or just check headers.
        // Let's use a custom header "X-Test-Role" to decide roles.

        var role = Context.Request.Headers["X-Test-Role"].ToString();
        var userId = Context.Request.Headers["X-Test-UserId"].ToString();

        if (string.IsNullOrEmpty(role) && string.IsNullOrEmpty(userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "TestUser"),
            new Claim("CompanyId", "1") // Default CompanyId
        };

        if (!string.IsNullOrEmpty(role))
        {
            foreach (var r in role.Split(','))
            {
                claims.Add(new Claim(ClaimTypes.Role, r.Trim()));
            }
        }
        
        if (!string.IsNullOrEmpty(userId)) 
        {
            claims.Add(new Claim("UserId", userId));
        }

        var identity = new ClaimsIdentity(claims, DefaultScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, DefaultScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
