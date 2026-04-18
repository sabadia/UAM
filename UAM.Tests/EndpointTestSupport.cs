using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Net.Http.Json;
using System.Text.Json;

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Slogtry.Abstractions;
using Slogtry.Grpc;
using UAM.Context;

namespace UAM.Tests;

public sealed class TestWebApplicationFactory : IDisposable
{
    private const string TestJwtIssuer = "uam-tests";
    private const string TestJwtAudience = "uam-tests-clients";
    private readonly string _databaseName = $"uam-tests-{Guid.NewGuid():N}";
    private readonly WebApplication _app;

    public TestWebApplicationFactory()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
            ApplicationName = typeof(Program).Assembly.FullName
        });

        builder.WebHost.UseTestServer();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=uam-test;Username=test;Password=test",
            ["Security:Jwt:Issuer"] = TestJwtIssuer,
            ["Security:Jwt:Audience"] = TestJwtAudience,
            ["Security:Jwt:RequireHttpsMetadata"] = "false",
            ["RemoteServices:Tenant:GrpcEndpoint"] = "https://localhost:7001",
            ["RemoteServices:Identity:GrpcEndpoint"] = "https://localhost:7002"
        });

        Program.ConfigureServices(builder);

        builder.Services.RemoveAll<DbContextOptions<AppDbContext>>();
        builder.Services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
        builder.Services.RemoveAll<AppDbContext>();
        builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        builder.Services.RemoveAll<ITenantDirectoryClient>();
        builder.Services.RemoveAll<IIdentityAccessClient>();
        builder.Services.AddScoped<ITenantDirectoryClient, FakeTenantDirectoryClient>();
        builder.Services.AddScoped<IIdentityAccessClient, FakeIdentityAccessClient>();

        _app = builder.Build();
        Program.ConfigurePipeline(_app);
        _app.StartAsync().GetAwaiter().GetResult();
        TestTokens.Initialize(TestJwtIssuer, TestJwtAudience);
    }

    public HttpClient CreateClient()
    {
        return _app.GetTestClient();
    }

    public void Dispose()
    {
        _app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}

internal static class EndpointTestJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}

internal static class EndpointTestHttp
{
    public static HttpRequestMessage CreateJsonRequest(HttpMethod method, string uri, string tenantId, object? body = null)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("X-Tenant-Id", tenantId);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.ForTenant(tenantId));

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: EndpointTestJson.Options);
        }

        return request;
    }
}

internal static class TestTokens
{
    private static string _issuer = string.Empty;
    private static string _audience = string.Empty;

    public static string Default => ForTenant("tenant-a");

    public static void Initialize(string issuer, string audience)
    {
        _issuer = issuer;
        _audience = audience;
    }

    public static string ForTenant(string tenantId)
    {
        var signingCredentials = new SigningCredentials(FakeTenantDirectoryClient.GetTenantPrivateKey(tenantId), SecurityAlgorithms.RsaSha256);
        return CreateToken("user-1", tenantId, _issuer, _audience, signingCredentials, includeTenantIdClaim: true);
    }

    public static string ForTenantWithoutTenantIdClaim(string tenantId)
    {
        var signingCredentials = new SigningCredentials(FakeTenantDirectoryClient.GetTenantPrivateKey(tenantId), SecurityAlgorithms.RsaSha256);
        return CreateToken("user-1", tenantId, _issuer, _audience, signingCredentials, includeTenantIdClaim: false);
    }

    public static string ForTenantWithExplicitUserIdClaim(string tenantId, string explicitUserId, string subjectUserId = "user-1")
    {
        var signingCredentials = new SigningCredentials(FakeTenantDirectoryClient.GetTenantPrivateKey(tenantId), SecurityAlgorithms.RsaSha256);
        return CreateToken(subjectUserId, tenantId, _issuer, _audience, signingCredentials, includeTenantIdClaim: true, explicitUserId);
    }

    private static string CreateToken(
        string userId,
        string tenantId,
        string issuer,
        string audience,
        SigningCredentials credentials,
        bool includeTenantIdClaim,
        string? explicitUserIdClaim = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.NameIdentifier, userId),
            new("scope", "uam.api")
        };

        if (includeTenantIdClaim)
            claims.Add(new(TenancyConstants.TenantClaimName, tenantId));

        if (!string.IsNullOrWhiteSpace(explicitUserIdClaim))
            claims.Add(new("user_id", explicitUserIdClaim!));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

internal sealed class FakeTenantDirectoryClient : ITenantDirectoryClient
{
    private static readonly Dictionary<string, RsaSecurityKey> PrivateKeys = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> PublicKeys = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object Sync = new();

    public Task<TenantLookupResult> GetTenantAsync(string tenantId, CancellationToken cancellationToken)
    {
        if (tenantId == "tenant-unknown")
            return Task.FromResult(new TenantLookupResult(false, false));

        if (tenantId == "tenant-inactive")
            return Task.FromResult(new TenantLookupResult(true, false));

        return Task.FromResult(new TenantLookupResult(true, true));
    }

    public Task<TenantSigningKeyResult> GetTenantSigningKeyAsync(string tenantId, string? keyId, CancellationToken cancellationToken)
    {
        var (_, publicKeyPem) = GetOrCreateTenantKeys(tenantId);
        var id = keyId ?? tenantId;

        if (tenantId == "tenant-inactive")
            return Task.FromResult(new TenantSigningKeyResult(id, true, false, publicKeyPem, string.Empty));

        if (tenantId == "tenant-unknown")
            return Task.FromResult(new TenantSigningKeyResult(id, false, false, publicKeyPem, string.Empty));

        return Task.FromResult(new TenantSigningKeyResult(id, true, true, publicKeyPem, string.Empty));
    }

    public Task<TenantSigningKeyResult> GetTenantSigningKeyInternalAsync(string tenantId, string? keyId, CancellationToken cancellationToken)
    {
        return GetTenantSigningKeyAsync(tenantId, keyId, cancellationToken);
    }

    public static SecurityKey GetTenantPrivateKey(string tenantId)
    {
        var (privateKey, _) = GetOrCreateTenantKeys(tenantId);
        return privateKey;
    }

    private static (RsaSecurityKey privateKey, string publicKeyPem) GetOrCreateTenantKeys(string tenantId)
    {
        lock (Sync)
        {
            if (PrivateKeys.TryGetValue(tenantId, out var privateKey)
                && PublicKeys.TryGetValue(tenantId, out var publicKeyPem))
            {
                return (privateKey, publicKeyPem);
            }

            var rsa = RSA.Create(2048);
            privateKey = new RsaSecurityKey(rsa);
            publicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();
            PrivateKeys[tenantId] = privateKey;
            PublicKeys[tenantId] = publicKeyPem;
            return (privateKey, publicKeyPem);
        }
    }
}

internal sealed class FakeIdentityAccessClient : IIdentityAccessClient
{
    public Task<TenantAccessResult> AuthorizeTenantAccessAsync(string tenantId, string userId, CancellationToken cancellationToken)
    {
        var allowed = tenantId != "tenant-forbidden" && userId != "user-forbidden";
        return Task.FromResult(new TenantAccessResult(allowed, allowed ? ["editor"] : [], allowed ? ["users.write"] : []));
    }
}
