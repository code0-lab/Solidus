using System.Security.Claims;
using System.Text.Encodings.Web;
using DomusMercatoris.Service.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace DomusMercatorisDotnetRest.Authentication
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly ApiKeyService _apiKeyService;
        public const string SchemeName = "ApiKey";
        public const string HeaderName = "X-API-KEY";

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            ApiKeyService apiKeyService)
            : base(options, logger, encoder, clock)
        {
            _apiKeyService = apiKeyService;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(HeaderName, out var apiKeyHeaderValues))
            {
                return AuthenticateResult.NoResult();
            }

            var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
            if (string.IsNullOrEmpty(providedApiKey))
            {
                return AuthenticateResult.NoResult();
            }

            var companyId = await _apiKeyService.ValidateApiKeyAsync(providedApiKey);

            if (companyId == null)
            {
                return AuthenticateResult.Fail("Invalid API Key");
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, $"Company-{companyId}"),
                new Claim("CompanyId", companyId.Value.ToString()),
                new Claim(ClaimTypes.Role, "CompanyApi") 
            };

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return AuthenticateResult.Success(ticket);
        }
    }
}
