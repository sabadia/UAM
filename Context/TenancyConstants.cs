namespace UAM.Context;

public static class TenancyConstants
{
    public const string TenantHeaderName = "X-Tenant-Id";
    public const string TenantClaimName = "TenantId";
    public const string ApiPrefix = "/api/v1";
    public const string HealthPath = "/api/v1/health";
    public const string TenantContextItemKey = "__tenant-context";
}