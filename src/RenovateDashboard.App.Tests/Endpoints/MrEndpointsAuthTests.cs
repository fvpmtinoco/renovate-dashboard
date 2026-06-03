using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RenovateDashboard.App.Tests.Endpoints;

public class MrEndpointsAuthTests : IClassFixture<MrEndpointsAuthTests.AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public MrEndpointsAuthTests(AuthTestFactory factory) => _factory = factory;

    [Fact]
    public async Task GetMrs_WithoutToken_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/mrs");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public class AuthTestFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Provide config so the host starts without contacting Auth0 or GitLab.
            // An unauthenticated request is rejected before either is touched.
            builder.UseSetting("Auth0:Domain", "test.eu.auth0.com");
            builder.UseSetting("Auth0:Audience", "https://test-api");
            builder.UseSetting("GitLab:Repos", "");
        }
    }
}
