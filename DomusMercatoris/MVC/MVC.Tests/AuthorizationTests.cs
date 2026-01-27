using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MVC.Tests;

public class AuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAuthenticatedClient(string role)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(TestAuthHandler.DefaultScheme)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.DefaultScheme, options => { });
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Get_Categories_AsManager_ReturnsSuccess()
    {
        // Arrange
        var client = CreateAuthenticatedClient("Manager");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Manager"); // Handler will pick this up

        // Act
        var response = await client.GetAsync("/Categories");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Get_Categories_AsUser_ReturnsForbiddenOrRedirect()
    {
        // Arrange
        var client = CreateAuthenticatedClient("User");
        client.DefaultRequestHeaders.Add("X-Test-Role", "User");
        client.DefaultRequestHeaders.Add("X-Test-UserId", "999"); // Random ID to ensure DB check fails

        // Act
        var response = await client.GetAsync("/Categories");

        // Assert
        // The policy failure usually results in Forbidden (403) or Redirect to Access Denied path
        // Depending on default challenge scheme.
        // Since we are adding TestScheme, it might return 403.
        // Or if Cookie auth is still active as default challenge, it might redirect to Login.
        
        // Let's check for either 403 or Redirect
        Assert.True(
            response.StatusCode == HttpStatusCode.Forbidden || 
            response.StatusCode == HttpStatusCode.Redirect,
            $"Expected Forbidden or Redirect, but got {response.StatusCode}");
    }
}
