using System.Net;
using FluentAssertions;
using UAM.Dtos.Stories;
using Xunit;

namespace UAM.Tests;

public sealed class StoryRoutesTests(TestWebApplicationFactory factory) : EndpointTestBase(factory), IClassFixture<TestWebApplicationFactory>
{
    [Theory]
    [MemberData(nameof(EndpointTheoryData.PaginationNormalizationCases), MemberType = typeof(EndpointTheoryData))]
    public async Task List_NormalizesPaginationInput(int offset, int limit, int expectedOffset, int expectedLimit)
    {
        var tenantId = $"tenant-story-list-{Guid.NewGuid():N}";

        var response = await SendAsync(HttpMethod.Get, $"/api/v1/stories?offset={offset}&limit={limit}&search=%20%20", tenantId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await ReadPagedAsync<StoryResponse>(response);
        page.Offset.Should().Be(expectedOffset);
        page.Limit.Should().Be(expectedLimit);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenSlugIsDuplicate()
    {
        var tenantId = $"tenant-story-dup-{Guid.NewGuid():N}";

        (await SendAsync(HttpMethod.Post, "/api/v1/stories", tenantId, new { title = "First", slug = "duplicate-slug" })).StatusCode
            .Should().Be(HttpStatusCode.OK);

        var duplicate = await SendAsync(HttpMethod.Post, "/api/v1/stories", tenantId, new { title = "Second", slug = "duplicate-slug" });

        duplicate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_SearchIsCaseInsensitive()
    {
        var tenantId = $"tenant-story-search-{Guid.NewGuid():N}";

        await SendAsync(HttpMethod.Post, "/api/v1/stories", tenantId, new { title = "CaseSensitive Story", slug = $"story-{Guid.NewGuid():N}" });
        await SendAsync(HttpMethod.Post, "/api/v1/stories", tenantId, new { title = "Another Story", slug = $"story-{Guid.NewGuid():N}" });

        var response = await SendAsync(HttpMethod.Get, "/api/v1/stories?search=casesensitive", tenantId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await ReadPagedAsync<StoryResponse>(response);
        page.Items.Should().ContainSingle(item => item.Title == "CaseSensitive Story");
    }

    [Theory]
    [MemberData(nameof(EndpointTheoryData.StoryMissingEndpointCases), MemberType = typeof(EndpointTheoryData))]
    public async Task MutationEndpoints_ReturnNotFound_WhenStoryDoesNotExist(string caseKey)
    {
        var tenantId = $"tenant-story-missing-endpoint-{Guid.NewGuid():N}";
        var response = await SendMissingStoryCaseAsync(caseKey, tenantId);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("views", 0)]
    [InlineData("views", -1)]
    [InlineData("likes", 0)]
    [InlineData("likes", -1)]
    [InlineData("dislikes", 0)]
    [InlineData("dislikes", -1)]
    public async Task CounterEndpoints_ReturnBadRequest_WhenDeltaIsNonPositive(string counterRoute, int delta)
    {
        var tenantId = $"tenant-story-counter-invalid-{Guid.NewGuid():N}";
        var storyId = await CreateStoryAsync(tenantId);

        var response = await SendAsync(HttpMethod.Post, $"/api/v1/stories/{storyId}/{counterRoute}", tenantId, new { delta });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddView_IncrementsCounter_WhenDeltaIsPositive()
    {
        var tenantId = $"tenant-story-counter-happy-{Guid.NewGuid():N}";
        var storyId = await CreateStoryAsync(tenantId);

        var response = await SendAsync(HttpMethod.Post, $"/api/v1/stories/{storyId}/views", tenantId, new { delta = 2 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadApiAsync<StoryResponse>(response);
        payload.Data!.ViewCount.Should().Be(2);
    }

    [Theory]
    [MemberData(nameof(EndpointTheoryData.StoryUpdatePayloadCases), MemberType = typeof(EndpointTheoryData))]
    public async Task Update_PayloadPartitions(string caseKey)
    {
        var tenantId = $"tenant-story-update-{Guid.NewGuid():N}";
        var storyId = await CreateStoryAsync(tenantId, "seed-story");
        await CreateStoryAsync(tenantId, "other-story");

        var response = await (caseKey switch
        {
            "trim-fields" => SendAsync(HttpMethod.Put, $"/api/v1/stories/{storyId}", tenantId, new { title = "  Updated Story  ", slug = "  updated-story  " }),
            "duplicate-slug" => SendAsync(HttpMethod.Put, $"/api/v1/stories/{storyId}", tenantId, new { title = "Updated Story", slug = "other-story" }),
            _ => throw new ArgumentOutOfRangeException(nameof(caseKey), caseKey, "Unknown story update case")
        });

        if (caseKey == "trim-fields")
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var payload = await ReadApiAsync<StoryResponse>(response);
            payload.Data!.Title.Should().Be("Updated Story");
            payload.Data.Slug.Should().Be("updated-story");
            return;
        }

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [MemberData(nameof(EndpointTheoryData.DeleteRestoreSequenceCases), MemberType = typeof(EndpointTheoryData))]
    public async Task DeleteRestore_SequenceCases(string caseKey)
    {
        var tenantId = $"tenant-story-seq-{Guid.NewGuid():N}";
        var storyId = await CreateStoryAsync(tenantId);

        (await SendAsync(HttpMethod.Delete, $"/api/v1/stories/{storyId}", tenantId)).StatusCode.Should().Be(HttpStatusCode.OK);

        if (caseKey == "delete-restore")
        {
            (await SendAsync(HttpMethod.Get, $"/api/v1/stories/{storyId}", tenantId)).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await SendAsync(HttpMethod.Post, $"/api/v1/stories/{storyId}/restore", tenantId)).StatusCode.Should().Be(HttpStatusCode.OK);
            (await SendAsync(HttpMethod.Get, $"/api/v1/stories/{storyId}", tenantId)).StatusCode.Should().Be(HttpStatusCode.OK);
            return;
        }

        (await SendAsync(HttpMethod.Delete, $"/api/v1/stories/{storyId}", tenantId)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<HttpResponseMessage> SendMissingStoryCaseAsync(string caseKey, string tenantId)
    {
        return caseKey switch
        {
            "publish" => await SendAsync(HttpMethod.Post, "/api/v1/stories/missing/publish", tenantId),
            "unpublish" => await SendAsync(HttpMethod.Delete, "/api/v1/stories/missing/publish", tenantId),
            "views" => await SendAsync(HttpMethod.Post, "/api/v1/stories/missing/views", tenantId, new { delta = 1 }),
            "likes" => await SendAsync(HttpMethod.Post, "/api/v1/stories/missing/likes", tenantId, new { delta = 1 }),
            "dislikes" => await SendAsync(HttpMethod.Post, "/api/v1/stories/missing/dislikes", tenantId, new { delta = 1 }),
            _ => throw new ArgumentOutOfRangeException(nameof(caseKey), caseKey, "Unknown story endpoint case")
        };
    }

    private async Task<string> CreateStoryAsync(string tenantId, string? slug = null)
    {
        var normalizedSlug = slug ?? $"story-{Guid.NewGuid():N}";
        var response = await SendAsync(HttpMethod.Post, "/api/v1/stories", tenantId, new { title = normalizedSlug, slug = normalizedSlug });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadApiAsync<StoryResponse>(response);
        payload.Data.Should().NotBeNull();
        return payload.Data!.Id;
    }
}
