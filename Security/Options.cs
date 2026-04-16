namespace UAM.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Security:Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public bool RequireHttpsMetadata { get; set; } = false;
}

public sealed class RemoteServicesOptions
{
    public const string SectionName = "RemoteServices";

    public string TenantGrpcEndpoint { get; set; } = string.Empty;
    public string IdentityGrpcEndpoint { get; set; } = string.Empty;
}
