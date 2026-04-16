using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace UAM.Tests;

public sealed class EndpointPlanTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task HealthCheck_Works_WithoutTenantHeader()
    {
        var response = await _client.GetAsync("/api/v1/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(EndpointTestJson.Options);
        json.TryGetProperty("status", out var status).Should().BeTrue();
        status.GetString().Should().Be("Healthy");
    }

    [Theory]
    [InlineData("/api/v1/stories")]
    public async Task ApiRoutes_RequireTenantHeader(string route)
    {
        var response = await _client.GetAsync(route);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(EndpointTestJson.Options);
        json.GetProperty("title").GetString().Should().Be("Missing tenant header");
    }

    [Fact]
    public async Task ApiRoutes_RequireAuthentication()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/stories");
        request.Headers.Add("X-Tenant-Id", "tenant-a");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(EndpointTestJson.Options);
        json.GetProperty("title").GetString().Should().Be("Authentication required");
    }

    [Fact]
    public async Task ApiRoutes_RejectUnknownTenant()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/stories");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.ForTenant("tenant-unknown"));
        request.Headers.Add("X-Tenant-Id", "tenant-unknown");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(EndpointTestJson.Options);
        json.GetProperty("title").GetString().Should().Be("Tenant not found");
    }
    
    [Fact]
    public async Task ApiRoutes_AllowTenantFromTokenClaim_WithoutHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/stories");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.ForTenant("tenant-a"));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ApiRoutes_FallbackToHeader_WhenTokenHasNoTenantClaim()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/stories");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.ForTenantWithoutTenantIdClaim("tenant-a"));
        request.Headers.Add("X-Tenant-Id", "tenant-a");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ApiRoutes_RejectMismatchedTenantClaimAndHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/stories");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.ForTenant("tenant-a"));
        request.Headers.Add("X-Tenant-Id", "tenant-b");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(EndpointTestJson.Options);
        json.GetProperty("title").GetString().Should().Be("Tenant mismatch");
    }
}

