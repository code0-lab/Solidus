using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MVC.Tests;

public class BasicSecurityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BasicSecurityTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }


    [Theory]
    [InlineData("/")]
    public async Task Get_EndpointsReturnSuccessAndCorrectContentType(string url)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        Assert.Equal("text/html; charset=utf-8", 
            response.Content.Headers.ContentType.ToString());
    }

    [Theory]
    [InlineData("/Dashboard")]
    public async Task Get_ProtectedEndpoints_RedirectsToLogin(string url)
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync(url);


        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location.ToString();
        Assert.Contains("/?ReturnUrl=", location);
    }
}
