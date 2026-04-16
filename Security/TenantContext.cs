namespace UAM.Security;

public sealed record TenantContext(
    string TenantId,
    string UserId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public interface ITenantContextAccessor
{
    TenantContext? Current { get; }
    void SetCurrent(TenantContext context);
}

public sealed class TenantContextAccessor : ITenantContextAccessor
{
    public TenantContext? Current { get; private set; }

    public void SetCurrent(TenantContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Current = context;
    }
}
