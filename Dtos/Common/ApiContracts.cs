namespace UAM.Dtos.Common;

using Slogtry.Abstractions;

public enum SortDirection
{
    Asc,
    Desc
}

public static class StrictEnumInput
{
    public static SortDirection ParseSortDirection(string? value, string fieldName = "sortDirection")
    {
        if (string.IsNullOrWhiteSpace(value)) return SortDirection.Desc;

        var normalized = value.Trim();
        if (normalized != normalized.ToLowerInvariant())
            throw new InvalidOperationException($"{fieldName} must be lowercase.");

        return normalized switch
        {
            "asc" => SortDirection.Asc,
            "desc" => SortDirection.Desc,
            _ => throw new InvalidOperationException($"{fieldName} must be one of: asc, desc.")
        };
    }

}

public sealed record OffsetPaginationQuery(
    int Offset = 0,
    int Limit = 20,
    string? Search = null,
    string? SortBy = null,
    string SortDirection = "desc")
{
    public int NormalizedOffset => PaginationDefaults.Normalize(Offset, Limit).offset;

    public int NormalizedLimit => PaginationDefaults.Normalize(Offset, Limit).limit;

    public string? NormalizedSearch => string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();

    public SortDirection ParsedSortDirection => StrictEnumInput.ParseSortDirection(SortDirection);
}

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Offset, int Limit, long TotalCount)
{
    public int Count => Items.Count;

    public bool HasMore => Offset + Count < TotalCount;
}

public sealed record ApiResponse<T>(bool Success, T? Data, string? Message = null, IReadOnlyList<string>? Errors = null)
{
    public static ApiResponse<T> Ok(T data, string? message = null)
    {
        return new ApiResponse<T>(true, data, message);
    }

    public static ApiResponse<T> Fail(string message, params string[] errors)
    {
        return new ApiResponse<T>(false, default, message, errors);
    }
}
