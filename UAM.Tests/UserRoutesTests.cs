using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using UAM.Dtos.Users;
using Xunit;

namespace UAM.Tests;

public sealed class UserRoutesTests(TestWebApplicationFactory factory) : EndpointTestBase(factory), IClassFixture<TestWebApplicationFactory>
{
    [Theory]
    [MemberData(nameof(EndpointTheoryData.PaginationNormalizationCases), MemberType = typeof(EndpointTheoryData))]
    public async Task List_NormalizesPaginationInput(int offset, int limit, int expectedOffset, int expectedLimit)
    {
        var tenantId = $"tenant-user-list-{Guid.NewGuid():N}";

        var response = await SendAsync(HttpMethod.Get, $"/api/v1/users?offset={offset}&limit={limit}&search=%20%20", tenantId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await ReadPagedAsync<UserResponse>(response);
        page.Offset.Should().Be(expectedOffset);
        page.Limit.Should().Be(expectedLimit);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenEmailIsDuplicate()
    {
        var tenantId = $"tenant-user-dup-{Guid.NewGuid():N}";

        (await SendAsync(HttpMethod.Post, "/api/v1/users", tenantId, new
        {
            externalAuthUserId = $"external-{Guid.NewGuid():N}",
            email = "duplicate@example.com",
            displayName = "First User"
        })).StatusCode.Should().Be(HttpStatusCode.OK);

        var duplicate = await SendAsync(HttpMethod.Post, "/api/v1/users", tenantId, new
        {
            externalAuthUserId = $"external-{Guid.NewGuid():N}",
            email = "duplicate@example.com",
            displayName = "Second User"
        });

        duplicate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_SearchIsCaseInsensitive()
    {
        var tenantId = $"tenant-user-search-{Guid.NewGuid():N}";

        await SendAsync(HttpMethod.Post, "/api/v1/users", tenantId, new
        {
            externalAuthUserId = $"external-{Guid.NewGuid():N}",
            email = "casesensitive@example.com",
            displayName = "CaseSensitive User"
        });
        await SendAsync(HttpMethod.Post, "/api/v1/users", tenantId, new
        {
            externalAuthUserId = $"external-{Guid.NewGuid():N}",
            email = "another@example.com",
            displayName = "Another User"
        });

        var response = await SendAsync(HttpMethod.Get, "/api/v1/users?search=casesensitive", tenantId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await ReadPagedAsync<UserResponse>(response);
        page.Items.Should().ContainSingle(item => item.DisplayName == "CaseSensitive User");
    }

    [Theory]
    [MemberData(nameof(EndpointTheoryData.UserMissingEndpointCases), MemberType = typeof(EndpointTheoryData))]
    public async Task MutationEndpoints_ReturnNotFound_WhenUserDoesNotExist(string caseKey)
    {
        var tenantId = $"tenant-user-missing-{Guid.NewGuid():N}";
        var response = await SendMissingUserCaseAsync(caseKey, tenantId);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_UpdatesProvidedFieldsOnly()
    {
        var tenantId = $"tenant-user-patch-{Guid.NewGuid():N}";
        var userId = await CreateUserAsync(tenantId, "patch.user@example.com");

        var patchResponse = await SendAsync(HttpMethod.Patch, $"/api/v1/users/{userId}", tenantId, new
        {
            displayName = "Patched User",
            preferences = new
            {
                theme = "dark",
                smsNotificationsEnabled = true
            }
        });

        patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var patched = await ReadApiAsync<UserResponse>(patchResponse);
        patched.Data!.DisplayName.Should().Be("Patched User");
        patched.Data.Preferences.Theme.Should().Be(UserTheme.Dark);
        patched.Data.Preferences.SmsNotificationsEnabled.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(EndpointTheoryData.DeleteRestoreSequenceCases), MemberType = typeof(EndpointTheoryData))]
    public async Task DeleteRestore_SequenceCases(string caseKey)
    {
        var tenantId = $"tenant-user-seq-{Guid.NewGuid():N}";
        var userId = await CreateUserAsync(tenantId);

        (await SendAsync(HttpMethod.Delete, $"/api/v1/users/{userId}", tenantId)).StatusCode.Should().Be(HttpStatusCode.OK);

        if (caseKey == "delete-restore")
        {
            (await SendAsync(HttpMethod.Get, $"/api/v1/users/{userId}", tenantId)).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await SendAsync(HttpMethod.Post, $"/api/v1/users/{userId}/restore", tenantId)).StatusCode.Should().Be(HttpStatusCode.OK);
            (await SendAsync(HttpMethod.Get, $"/api/v1/users/{userId}", tenantId)).StatusCode.Should().Be(HttpStatusCode.OK);
            return;
        }

        (await SendAsync(HttpMethod.Delete, $"/api/v1/users/{userId}", tenantId)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ActivateDeactivate_UpdatesStatus()
    {
        var tenantId = $"tenant-user-status-{Guid.NewGuid():N}";
        var userId = await CreateUserAsync(tenantId);

        var deactivate = await SendAsync(HttpMethod.Post, $"/api/v1/users/{userId}/deactivate", tenantId);
        deactivate.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadApiAsync<UserResponse>(deactivate)).Data!.IsActive.Should().BeFalse();

        var activate = await SendAsync(HttpMethod.Post, $"/api/v1/users/{userId}/activate", tenantId);
        activate.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadApiAsync<UserResponse>(activate)).Data!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Preferences_PutThenGet_RoundTrips()
    {
        var tenantId = $"tenant-user-pref-{Guid.NewGuid():N}";
        var userId = await CreateUserAsync(tenantId);

        var put = await SendAsync(HttpMethod.Put, $"/api/v1/users/{userId}/preferences", tenantId, new
        {
            language = "bn",
            timeZone = "Asia/Dhaka",
            theme = "light",
            emailNotificationsEnabled = false,
            smsNotificationsEnabled = true
        });

        put.StatusCode.Should().Be(HttpStatusCode.OK);

        var get = await SendAsync(HttpMethod.Get, $"/api/v1/users/{userId}/preferences", tenantId);
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadApiAsync<UserPreferencesResponse>(get);
        payload.Data!.Language.Should().Be("bn");
        payload.Data.TimeZone.Should().Be("Asia/Dhaka");
        payload.Data.Theme.Should().Be(UserTheme.Light);
        payload.Data.EmailNotificationsEnabled.Should().BeFalse();
        payload.Data.SmsNotificationsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Me_GetsCurrentUserBySubjectClaim()
    {
        var tenantId = $"tenant-user-me-sub-{Guid.NewGuid():N}";
        var externalId = "user-1";

        await SendAsync(HttpMethod.Post, "/api/v1/users", tenantId, new
        {
            externalAuthUserId = externalId,
            email = $"me-sub-{Guid.NewGuid():N}@example.com",
            displayName = "Current User"
        });

        var response = await SendAsync(HttpMethod.Get, "/api/v1/me", tenantId);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadApiAsync<UserResponse>(response);
        payload.Data!.ExternalAuthUserId.Should().Be(externalId);
    }

    [Fact]
    public async Task Me_UsesExplicitUserIdClaim_WhenPresent()
    {
        var tenantId = $"tenant-user-me-explicit-{Guid.NewGuid():N}";
        var firstUserId = await CreateUserAsync(tenantId, $"first-{Guid.NewGuid():N}@example.com", externalAuthUserId: "user-1");
        _ = await CreateUserAsync(tenantId, $"second-{Guid.NewGuid():N}@example.com", externalAuthUserId: "user-2");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me");
        request.Headers.Add("X-Tenant-Id", tenantId);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.ForTenantWithExplicitUserIdClaim(tenantId, firstUserId, "user-2"));

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadApiAsync<UserResponse>(response);
        payload.Data!.Id.Should().Be(firstUserId);
    }

    private async Task<HttpResponseMessage> SendMissingUserCaseAsync(string caseKey, string tenantId)
    {
        return caseKey switch
        {
            "update" => await SendAsync(HttpMethod.Put, "/api/v1/users/missing", tenantId, new { email = "missing@example.com", displayName = "Missing User" }),
            "patch" => await SendAsync(HttpMethod.Patch, "/api/v1/users/missing", tenantId, new { displayName = "Missing User" }),
            "delete" => await SendAsync(HttpMethod.Delete, "/api/v1/users/missing", tenantId),
            "restore" => await SendAsync(HttpMethod.Post, "/api/v1/users/missing/restore", tenantId),
            "activate" => await SendAsync(HttpMethod.Post, "/api/v1/users/missing/activate", tenantId),
            "deactivate" => await SendAsync(HttpMethod.Post, "/api/v1/users/missing/deactivate", tenantId),
            "get-preferences" => await SendAsync(HttpMethod.Get, "/api/v1/users/missing/preferences", tenantId),
            "put-preferences" => await SendAsync(HttpMethod.Put, "/api/v1/users/missing/preferences", tenantId, new { language = "en", timeZone = "UTC", theme = "system" }),
            _ => throw new ArgumentOutOfRangeException(nameof(caseKey), caseKey, "Unknown user endpoint case")
        };
    }

    private async Task<string> CreateUserAsync(string tenantId, string? email = null, string? externalAuthUserId = null)
    {
        var requestEmail = email ?? $"user-{Guid.NewGuid():N}@example.com";
        var requestExternalId = externalAuthUserId ?? $"external-{Guid.NewGuid():N}";
        var response = await SendAsync(HttpMethod.Post, "/api/v1/users", tenantId, new
        {
            externalAuthUserId = requestExternalId,
            email = requestEmail,
            displayName = requestEmail
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadApiAsync<UserResponse>(response);
        payload.Data.Should().NotBeNull();
        return payload.Data!.Id;
    }
}
