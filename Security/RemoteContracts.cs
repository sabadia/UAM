namespace UAM.Security;

public sealed record TenantLookupResult(bool Exists, bool IsActive);
public sealed record TenantSigningKeyResult(bool Exists, bool IsActive, string? PublicKeyPem);

public sealed record TenantAccessResult(
    bool IsAllowed,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public interface ITenantDirectoryClient
{
    Task<TenantLookupResult> GetTenantAsync(string tenantId, CancellationToken cancellationToken);
    Task<TenantSigningKeyResult> GetTenantSigningKeyAsync(string tenantId, string? keyId, CancellationToken cancellationToken);
}

public interface IIdentityAccessClient
{
    Task<TenantAccessResult> AuthorizeTenantAccessAsync(string tenantId, string userId, CancellationToken cancellationToken);
}

public sealed class RemoteDependencyException(string message, Exception? innerException = null) : Exception(message, innerException);
