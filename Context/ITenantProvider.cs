using UAM.Security;

namespace UAM.Context;

public interface ITenantProvider
{
    string GetRequiredTenantId();
}

public sealed class HttpContextTenantProvider(IHttpContextAccessor httpContextAccessor, ITenantContextAccessor tenantContextAccessor) : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    private readonly ITenantContextAccessor _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));

    public string GetRequiredTenantId()
    {
        if (!string.IsNullOrWhiteSpace(_tenantContextAccessor.Current?.TenantId))
            return _tenantContextAccessor.Current!.TenantId;

        var tenantId = _httpContextAccessor.HttpContext?.Request.Headers[TenancyConstants.TenantHeaderName].ToString();

        if (string.IsNullOrWhiteSpace(tenantId)) throw new InvalidOperationException($"Missing required tenant header '{TenancyConstants.TenantHeaderName}'.");

        return tenantId.Trim();
    }
}
