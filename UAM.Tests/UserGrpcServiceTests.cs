using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using UAM.Grpc.Users.V1;
using Xunit;

namespace UAM.Tests;

public sealed class UserGrpcServiceTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task CreateUser_ThenGetUser_Works()
    {
        var tenantId = $"tenant-grpc-create-{Guid.NewGuid():N}";
        var (client, channel) = CreateGrpcClient(tenantId);
        using (channel)
        {
            var created = await client.CreateUserAsync(new CreateUserRequest
            {
                ExternalAuthUserId = $"grpc-ext-{Guid.NewGuid():N}",
                Email = $"grpc-create-{Guid.NewGuid():N}@example.com",
                DisplayName = "Grpc Create User"
            });

            var loaded = await client.GetUserAsync(new GetUserRequest
            {
                UserId = created.User.Id
            });

            loaded.User.Email.Should().Be(created.User.Email);
            loaded.User.DisplayName.Should().Be("Grpc Create User");
        }
    }

    [Fact]
    public async Task PatchUser_UpdatesOnlyProvidedFields()
    {
        var tenantId = $"tenant-grpc-patch-{Guid.NewGuid():N}";
        var (client, channel) = CreateGrpcClient(tenantId);
        using (channel)
        {
            var created = await client.CreateUserAsync(new CreateUserRequest
            {
                ExternalAuthUserId = $"grpc-ext-{Guid.NewGuid():N}",
                Email = $"grpc-patch-{Guid.NewGuid():N}@example.com",
                DisplayName = "Before Patch"
            });

            var patched = await client.PatchUserAsync(new PatchUserRequest
            {
                UserId = created.User.Id,
                Patch = new UserPatch
                {
                    DisplayName = "After Patch"
                },
                PreferencesPatch = new UserPreferencesPatch
                {
                    Theme = UserTheme.Dark
                }
            });

            patched.User.DisplayName.Should().Be("After Patch");
            patched.User.Preferences.Theme.Should().Be(UserTheme.Dark);
        }
    }

    [Fact]
    public async Task DeleteRestoreUser_Transitions()
    {
        var tenantId = $"tenant-grpc-delete-{Guid.NewGuid():N}";
        var (client, channel) = CreateGrpcClient(tenantId);
        using (channel)
        {
            var created = await client.CreateUserAsync(new CreateUserRequest
            {
                ExternalAuthUserId = $"grpc-ext-{Guid.NewGuid():N}",
                Email = $"grpc-delete-{Guid.NewGuid():N}@example.com",
                DisplayName = "Grpc Delete User"
            });

            var deleted = await client.DeleteUserAsync(new DeleteUserRequest
            {
                UserId = created.User.Id
            });
            deleted.Success.Should().BeTrue();

            var notFound = await Assert.ThrowsAsync<RpcException>(() => client.GetUserAsync(new GetUserRequest
            {
                UserId = created.User.Id
            }).ResponseAsync);
            notFound.StatusCode.Should().Be(StatusCode.NotFound);

            var restored = await client.RestoreUserAsync(new RestoreUserRequest
            {
                UserId = created.User.Id
            });

            restored.User.Id.Should().Be(created.User.Id);
        }
    }

    [Fact]
    public async Task SearchUsers_ReturnsFilteredResults()
    {
        var tenantId = $"tenant-grpc-search-{Guid.NewGuid():N}";
        var (client, channel) = CreateGrpcClient(tenantId);
        using (channel)
        {
            await client.CreateUserAsync(new CreateUserRequest
            {
                ExternalAuthUserId = $"grpc-ext-{Guid.NewGuid():N}",
                Email = $"alpha-{Guid.NewGuid():N}@example.com",
                DisplayName = "Alpha User"
            });
            await client.CreateUserAsync(new CreateUserRequest
            {
                ExternalAuthUserId = $"grpc-ext-{Guid.NewGuid():N}",
                Email = $"beta-{Guid.NewGuid():N}@example.com",
                DisplayName = "Beta User"
            });

            var result = await client.SearchUsersAsync(new SearchUsersRequest
            {
                Search = "alpha",
                Offset = 0,
                Limit = 10
            });

            result.Users.Should().ContainSingle(user => user.DisplayName == "Alpha User");
        }
    }

    [Fact]
    public async Task GetUserProfileSummary_ReturnsProjection()
    {
        var tenantId = $"tenant-grpc-summary-{Guid.NewGuid():N}";
        var (client, channel) = CreateGrpcClient(tenantId);
        using (channel)
        {
            var created = await client.CreateUserAsync(new CreateUserRequest
            {
                ExternalAuthUserId = $"grpc-ext-{Guid.NewGuid():N}",
                Email = $"grpc-summary-{Guid.NewGuid():N}@example.com",
                DisplayName = "Summary User"
            });

            var summary = await client.GetUserProfileSummaryAsync(new GetUserProfileSummaryRequest
            {
                UserId = created.User.Id
            });

            summary.Summary.Id.Should().Be(created.User.Id);
            summary.Summary.DisplayName.Should().Be("Summary User");
            summary.Summary.Email.Should().Be(created.User.Email);
        }
    }

    private (UserAccess.UserAccessClient client, GrpcChannel channel) CreateGrpcClient(string tenantId)
    {
        var httpClient = factory.CreateClient();
        httpClient.DefaultRequestVersion = HttpVersion.Version20;
        httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.ForTenant(tenantId));
        httpClient.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);

        var address = httpClient.BaseAddress ?? new Uri("http://localhost");
        var channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
        {
            HttpClient = httpClient
        });

        return (new UserAccess.UserAccessClient(channel), channel);
    }
}
