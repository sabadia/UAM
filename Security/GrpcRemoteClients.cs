using Grpc.Core;
using Identity.Grpc.Identity.V1;
using Tenant.Grpc.Tenancy.V1;


namespace UAM.Security;

public sealed class TenantDirectoryGrpcClient(TenantDirectory.TenantDirectoryClient client) : ITenantDirectoryClient
{
    public async Task<TenantLookupResult> GetTenantAsync(string tenantId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.GetTenantAsync(new GetTenantRequest
            {
                TenantId = tenantId
            }, cancellationToken: cancellationToken);

            return new TenantLookupResult(response.Exists, response.Tenant?.IsActive ?? false);
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            throw new RemoteDependencyException("Tenant service is currently unavailable.", ex);
        }
    }

    public async Task<TenantSigningKeyResult> GetTenantSigningKeyAsync(string tenantId, string? keyId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.GetTenantSigningKeyAsync(new GetTenantSigningKeyRequest
            {
                TenantId = tenantId,
                KeyId = keyId ?? string.Empty
            }, cancellationToken: cancellationToken);

            return new TenantSigningKeyResult(response.Exists, response.IsActive, response.PublicKeyPem);
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            throw new RemoteDependencyException("Tenant service is currently unavailable.", ex);
        }
    }
}

public sealed class IdentityAccessGrpcClient(IdentityAccess.IdentityAccessClient client) : IIdentityAccessClient
{
    public async Task<TenantAccessResult> AuthorizeTenantAccessAsync(string tenantId, string userId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.AuthorizeTenantAccessAsync(new AuthorizeTenantAccessRequest
            {
                TenantId = tenantId,
                UserId = userId
            }, cancellationToken: cancellationToken);

            return new TenantAccessResult(response.IsAllowed, response.Roles.ToArray(), response.Permissions.ToArray());
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            throw new RemoteDependencyException("Identity service is currently unavailable.", ex);
        }
    }
}
