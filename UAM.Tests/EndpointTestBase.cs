using System.Net.Http.Json;
using FluentAssertions;
using UAM.Dtos.Common;

namespace UAM.Tests;

public abstract class EndpointTestBase(TestWebApplicationFactory factory)
{
    protected TestWebApplicationFactory Factory { get; } = factory;
    protected HttpClient Client { get; } = factory.CreateClient();

    protected async Task<HttpResponseMessage> SendAsync(HttpMethod method, string uri, string tenantId, object? body = null)
    {
        using var request = EndpointTestHttp.CreateJsonRequest(method, uri, tenantId, body);
        return await Client.SendAsync(request);
    }

    protected static async Task<ApiResponse<T>> ReadApiAsync<T>(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(EndpointTestJson.Options);
        payload.Should().NotBeNull();
        return payload!;
    }

    protected static async Task<PagedResponse<T>> ReadPagedAsync<T>(HttpResponseMessage response)
    {
        var payload = await ReadApiAsync<PagedResponse<T>>(response);
        payload.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        return payload.Data!;
    }
}

