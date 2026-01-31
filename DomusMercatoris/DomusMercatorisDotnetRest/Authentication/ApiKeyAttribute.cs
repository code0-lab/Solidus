using System;
using Microsoft.AspNetCore.Authorization;

namespace DomusMercatorisDotnetRest.Authentication
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiKeyAttribute : Attribute, IAuthorizeData
    {
        public string? Policy { get; set; }
        public string? Roles { get; set; }
        public string? AuthenticationSchemes { get; set; } = ApiKeyAuthenticationHandler.SchemeName;
    }
}
