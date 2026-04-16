using UAM.Dtos.Common;

namespace UAM.Dtos.Stories;

public sealed record StoryListQuery(
    int Offset = 0,
    int Limit = 20,
    string? Search = null,
    bool? IsPublished = null,
    bool IncludeDeleted = false,
    string? SortBy = null,
    string SortDirection = "desc"
)
{
    public OffsetPaginationQuery Pagination => new(Offset, Limit, Search, SortBy, SortDirection);
}

public sealed record StoryUpsertRequest(
    string Title,
    string Slug,
    string? Description = null,
    string? Value = null,
    bool IsPublished = false,
    DateTimeOffset? PublishedAt = null,
    int ViewCount = 0,
    int LikeCount = 0,
    int DislikeCount = 0);

public sealed record StoryResponse(
    string Id,
    string TenantId,
    string Title,
    string Slug,
    string? Description,
    string? Value,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    int ViewCount,
    int LikeCount,
    int DislikeCount,
    bool IsDeleted,
    DateTimeOffset? DeletedAt,
    string? DeletedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string UpdatedBy);

public sealed record StoryCounterRequest(int Delta = 1);
