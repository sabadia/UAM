using Microsoft.EntityFrameworkCore;
using UAM.Context;
using UAM.Models;

namespace UAM.Repositories;

public sealed class EfRepository<TEntity>(AppDbContext context) : IRepository<TEntity>
    where TEntity : BaseModel
{
    private readonly DbSet<TEntity> _set = context.Set<TEntity>();

    public IQueryable<TEntity> Query(bool includeDeleted = false)
        => includeDeleted ? _set.IgnoreQueryFilters() : _set;

    public Task<TEntity?> FindAsync(string id, CancellationToken cancellationToken, bool includeDeleted = false)
        => Query(includeDeleted).FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken, bool includeDeleted = false)
        => Query(includeDeleted).AnyAsync(entity => entity.Id == id, cancellationToken);

    public Task<long> CountAsync(IQueryable<TEntity> query, CancellationToken cancellationToken)
        => query.LongCountAsync(cancellationToken);

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken)
        => _set.AddAsync(entity, cancellationToken).AsTask();

    public void Remove(TEntity entity)
        => _set.Remove(entity);
}

