using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using UAM.Models;

namespace UAM.Services.Common;

internal static class ServiceGuards
{
    public static string? NormalizeSearch(string? search)
    {
        return string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLowerInvariant();
    }

    public static IQueryable<TEntity> ApplySearch<TEntity>(IQueryable<TEntity> query, string? search, params Expression<Func<TEntity, string?>>[] selectors)
    {
        var normalizedSearch = NormalizeSearch(search);
        if (normalizedSearch is null || selectors.Length == 0)
        {
            return query;
        }

        var parameter = Expression.Parameter(typeof(TEntity), "entity");
        Expression? searchBody = null;

        foreach (var selector in selectors)
        {
            var selectorBody = new ReplaceParameterVisitor(selector.Parameters[0], parameter).Visit(selector.Body)
                               ?? throw new InvalidOperationException("Failed to build search expression.");

            var notNull = Expression.NotEqual(selectorBody, Expression.Constant(null, typeof(string)));
            var toLower = Expression.Call(selectorBody, typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!);
            var contains = Expression.Call(toLower, typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!, Expression.Constant(normalizedSearch));
            var fieldMatch = Expression.AndAlso(notNull, contains);

            searchBody = searchBody is null ? fieldMatch : Expression.OrElse(searchBody, fieldMatch);
        }

        var predicate = Expression.Lambda<Func<TEntity, bool>>(searchBody!, parameter);
        return query.Where(predicate);
    }

    public static async Task EnsureExistsAsync<TEntity>(IQueryable<TEntity> query, string id, string entityName, CancellationToken cancellationToken)
        where TEntity : BaseModel
    {
        if (!await ExistsAsync(query, id, cancellationToken))
        {
            throw new InvalidOperationException($"{entityName} '{id}' does not exist.");
        }
    }

    public static Task<bool> ExistsAsync<TEntity>(IQueryable<TEntity> query, string id, CancellationToken cancellationToken)
        where TEntity : BaseModel
    {
        return query.AnyAsync(entity => entity.Id == id, cancellationToken);
    }

    public static async Task EnsureDoesNotExistAsync<TEntity>(IQueryable<TEntity> query, Expression<Func<TEntity, bool>> predicate, string errorMessage, CancellationToken cancellationToken)
        where TEntity : BaseModel
    {
        if (await query.AnyAsync(predicate, cancellationToken))
        {
            throw new InvalidOperationException(errorMessage);
        }
    }

    public static async Task EnsureAllExistAsync<TEntity>(IQueryable<TEntity> query, IReadOnlyCollection<string> ids, string entityName, CancellationToken cancellationToken)
        where TEntity : BaseModel
    {
        if (ids.Count == 0)
        {
            return;
        }

        var normalizedIds = ids.Distinct().ToArray();
        var foundIds = await query.Where(entity => normalizedIds.Contains(entity.Id)).Select(entity => entity.Id).ToListAsync(cancellationToken);
        if (foundIds.Count != normalizedIds.Length)
        {
            throw new InvalidOperationException($"One or more {entityName.ToLowerInvariant()}s do not exist.");
        }
    }

    private sealed class ReplaceParameterVisitor(ParameterExpression source, ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == source ? target : node;
        }
    }
}


