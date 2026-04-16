using UAM.Models;

namespace UAM.Repositories;

public interface IRepository<TEntity> where TEntity : BaseModel
{
    IQueryable<TEntity> Query(bool includeDeleted = false);

    Task<TEntity?> FindAsync(string id, CancellationToken cancellationToken, bool includeDeleted = false);

    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken, bool includeDeleted = false);

    Task<long> CountAsync(IQueryable<TEntity> query, CancellationToken cancellationToken);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken);

    void Remove(TEntity entity);
}

