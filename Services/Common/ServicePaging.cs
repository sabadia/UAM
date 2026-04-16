using Microsoft.EntityFrameworkCore;
using UAM.Dtos.Common;

namespace UAM.Services.Common;

internal static class ServicePaging
{
    public static async Task<PagedResponse<T>> ToPagedResponseAsync<T>(IQueryable<T> query, OffsetPaginationQuery pagination, CancellationToken cancellationToken)
    {
        _ = pagination.ParsedSortDirection;

        var offset = pagination.NormalizedOffset;
        var limit = pagination.NormalizedLimit;
        var totalCount = await query.LongCountAsync(cancellationToken);
        var items = await query.Skip(offset).Take(limit).ToListAsync(cancellationToken);

        return new PagedResponse<T>(items, offset, limit, totalCount);
    }
}