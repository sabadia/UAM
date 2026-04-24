using System.Net;
using FluentAssertions;
using Slogtry.Contracts.Events.User.V1;
using Slogtry.Events.Abstractions;
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

    [Fact]
    public async Task MePrivacy_Get_ReturnsDefaults_WhenNotPreviouslySet()
    {
        var tenantId = $"tenant-me-privacy-default-{Guid.NewGuid():N}";
        await CreateUserAsync(tenantId);

        var response = await SendAsync(HttpMethod.Get, "/api/v1/me/privacy", tenantId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadApiAsync<UserPrivacySettingsResponse>(response);
        payload.Data.Should().NotBeNull();
        payload.Data!.ProfileVisibility.Should().Be(PrivacyVisibilityLevel.Everyone);
        payload.Data.WhoCanMessage.Should().Be(PrivacyVisibilityLevel.Everyone);
        payload.Data.WhoCanMention.Should().Be(PrivacyVisibilityLevel.Everyone);
        payload.Data.AllowIndexing.Should().BeTrue();
        payload.Data.AllowNsfwInFeed.Should().BeFalse();
    }

    [Fact]
    public async Task MePrivacy_Patch_PersistsChanges()
    {
        var tenantId = $"tenant-me-privacy-patch-{Guid.NewGuid():N}";
        await CreateUserAsync(tenantId);

        var patch = await SendAsync(HttpMethod.Patch, "/api/v1/me/privacy", tenantId, new
        {
            profileVisibility = "followers",
            whoCanMessage = "nobody",
            whoCanMention = "followers",
            allowIndexing = false,
            allowNsfwInFeed = true
        });

        patch.StatusCode.Should().Be(HttpStatusCode.OK);

        var get = await SendAsync(HttpMethod.Get, "/api/v1/me/privacy", tenantId);
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadApiAsync<UserPrivacySettingsResponse>(get);
        payload.Data.Should().NotBeNull();
        payload.Data!.ProfileVisibility.Should().Be(PrivacyVisibilityLevel.Followers);
        payload.Data.WhoCanMessage.Should().Be(PrivacyVisibilityLevel.Nobody);
        payload.Data.WhoCanMention.Should().Be(PrivacyVisibilityLevel.Followers);
        payload.Data.AllowIndexing.Should().BeFalse();
        payload.Data.AllowNsfwInFeed.Should().BeTrue();
    }

    [Fact]
    public async Task PatchMe_PublishesUserProfileUpdatedV1_Event()
    {
        var tenantId = $"tenant-me-event-{Guid.NewGuid():N}";
        await CreateUserAsync(tenantId);
        Factory.EventPublisher.Clear();

        var response = await SendAsync(HttpMethod.Patch, "/api/v1/me", tenantId,
            new { displayName = "Updated Name", bio = "Updated Bio", handle = "updated_handle" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var events = Factory.EventPublisher.PublishedEvents.ToArray();
        events.Should().NotBeEmpty();
        events.Should().ContainSingle(e => e.EventType == "user.profile.updated.v1");

        var evt = events.Single(e => e.EventType == "user.profile.updated.v1");
        var dataProperty = evt.GetType().GetProperty("Data");
        dataProperty.Should().NotBeNull();
        var data = dataProperty!.GetValue(evt) as UserProfileUpdatedV1;
        data.Should().NotBeNull();
        data!.DisplayName.Should().Be("Updated Name");
        data.Bio.Should().Be("Updated Bio");
        data.Handle.Should().Be("updated_handle");
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
