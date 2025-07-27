using System;
using System.Linq.Expressions;

namespace server.Repositories;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(object id);

    Task<IEnumerable<T>> GetAllAsync(
        Expression<Func<T, bool>>? filter = null,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25
    );

    Task<(IEnumerable<T>, int)> GetAllWithCountAsync(
        Expression<Func<T, bool>>? filter = null,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25
    );

    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

    /// <summary>
    /// Detaches an entity from the context.
    /// This is useful for entities that are being tracked but you want to stop tracking them.
    /// </summary>
    /// <param name="entity">The entity to detach.</param>
    void Detach(T entity);

    Task AddAsync(T entity);

    /// <summary>
    /// Updates an entity in the repository.
    /// This method is used for mutable entities.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    void Update(T entity);

    void Remove(T entity);

    Task<int> SaveChangesAsync();

}
