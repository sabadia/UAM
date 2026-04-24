using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using UAM.Dtos.Users;
using Xunit;

namespace UAM.Tests;

public sealed class MePatchApiTests(TestWebApplicationFactory factory) : EndpointTestBase(factory), IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task PatchMe_UpdatesBioAndHandle_WhenValidInput()
    {
        var tenantId = $"tenant-me-patch-bio-{Guid.NewGuid():N}";
        await CreateUserAsync(tenantId);

        var response = await SendAsync(HttpMethod.Patch, "/api/v1/me", tenantId,
            new { bio = "Hello world", handle = "testuser123" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadApiAsync<UserResponse>(response);
        payload.Data.Should().NotBeNull();
        payload.Data!.Bio.Should().Be("Hello world");
        payload.Data.Handle.Should().Be("testuser123");
    }

    [Fact]
    public async Task PatchMe_Returns400_ForInvalidHandle()
    {
        var tenantId = $"tenant-me-patch-bad-handle-{Guid.NewGuid():N}";
        await CreateUserAsync(tenantId);

        var response = await SendAsync(HttpMethod.Patch, "/api/v1/me", tenantId,
            new { handle = "INVALID HANDLE!" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PatchMe_NormalizesHandleToLowercase()
    {
        var tenantId = $"tenant-me-patch-lower-{Guid.NewGuid():N}";
        await CreateUserAsync(tenantId);

        var response = await SendAsync(HttpMethod.Patch, "/api/v1/me", tenantId,
            new { handle = "ValidUser_99" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadApiAsync<UserResponse>(response);
        payload.Data!.Handle.Should().Be("validuser_99");
    }

    [Fact]
    public async Task PatchMe_UpdatesWebsite_WhenProvided()
    {
        var tenantId = $"tenant-me-patch-web-{Guid.NewGuid():N}";
        await CreateUserAsync(tenantId);

        var response = await SendAsync(HttpMethod.Patch, "/api/v1/me", tenantId,
            new { website = "https://example.com" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadApiAsync<UserResponse>(response);
        payload.Data!.Website.Should().Be("https://example.com");
    }

    [Fact]
    public async Task PatchMe_Returns400_ForHandleTooShort()
    {
        var tenantId = $"tenant-me-patch-short-{Guid.NewGuid():N}";
        await CreateUserAsync(tenantId);

        var response = await SendAsync(HttpMethod.Patch, "/api/v1/me", tenantId,
            new { handle = "ab" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task CreateUserAsync(string tenantId)
    {
        var response = await SendAsync(HttpMethod.Post, "/api/v1/users", tenantId, new
        {
            externalAuthUserId = "user-1",
            email = $"me-{Guid.NewGuid():N}@example.com",
            displayName = "Test User"
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
