using System.Text.Json;

namespace UAM.Bootstrap;

internal sealed class LowerCaseJsonNamingPolicy : JsonNamingPolicy
{
    public static LowerCaseJsonNamingPolicy Instance { get; } = new();

    public override string ConvertName(string name) => name.ToLowerInvariant();
}

