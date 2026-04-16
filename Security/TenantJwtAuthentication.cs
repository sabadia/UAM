using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UAM.Context;

namespace UAM.Security;

public interface ITenantSigningKeyProvider
{
    Task<TenantSigningKeyLookupResult> GetSigningKeyAsync(string tenantId, string? keyId, CancellationToken cancellationToken);
}

public sealed record TenantSigningKeyLookupResult(bool Exists, bool IsActive, SecurityKey? Key);

public sealed class TenantSigningKeyProvider(ITenantDirectoryClient tenantDirectoryClient, IMemoryCache cache) : ITenantSigningKeyProvider
{
    public async Task<TenantSigningKeyLookupResult> GetSigningKeyAsync(string tenantId, string? keyId, CancellationToken cancellationToken)
    {
        var cacheKey = $"tenant-jwt-key:{tenantId}:{keyId}";
        if (cache.TryGetValue(cacheKey, out TenantSigningKeyLookupResult? cached) && cached is not null)
            return cached;

        var response = await tenantDirectoryClient.GetTenantSigningKeyAsync(tenantId, keyId, cancellationToken);
        if (string.IsNullOrWhiteSpace(response.PublicKeyPem))
        {
            return new TenantSigningKeyLookupResult(response.Exists, response.IsActive, null);
        }

        var rsa = RSA.Create();
        rsa.ImportFromPem(response.PublicKeyPem);
        var result = new TenantSigningKeyLookupResult(response.Exists, response.IsActive, new RsaSecurityKey(rsa));
        cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        return result;
    }
}

public sealed class TenantJwtAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ITenantSigningKeyProvider signingKeyProvider,
    JwtOptions jwtOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var token = authHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
            return AuthenticateResult.NoResult();

        JwtSecurityToken jwtToken;
        try
        {
            jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
        }
        catch (Exception ex)
        {
            return AuthenticateResult.Fail($"Invalid token format: {ex.Message}");
        }

        var tokenTenantId = jwtToken.Claims
            .FirstOrDefault(claim => string.Equals(claim.Type, TenancyConstants.TenantClaimName, StringComparison.Ordinal))
            ?.Value
            .Trim();

        var headerTenantId = Request.Headers[TenancyConstants.TenantHeaderName].ToString().Trim();
        var tenantId = !string.IsNullOrWhiteSpace(tokenTenantId)
            ? tokenTenantId
            : headerTenantId;

        if (string.IsNullOrWhiteSpace(tenantId))
            return AuthenticateResult.NoResult();

        TenantSigningKeyLookupResult keyLookup;
        try
        {
            keyLookup = await signingKeyProvider.GetSigningKeyAsync(tenantId, jwtToken.Header.Kid, Context.RequestAborted);
        }
        catch (RemoteDependencyException ex)
        {
            return AuthenticateResult.Fail($"Signing key lookup failed: {ex.Message}");
        }

        if (keyLookup.Key is null)
            return AuthenticateResult.Fail("Signing key is not available for tenant.");

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = keyLookup.Key,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, tokenValidationParameters, out _);
            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }
        catch (Exception ex)
        {
            return AuthenticateResult.Fail($"Token validation failed: {ex.Message}");
        }
    }
}
