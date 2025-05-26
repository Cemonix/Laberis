using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using server.Data;

namespace server.Repositories;

public abstract class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly LaberisDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(LaberisDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = _context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(object id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 1000)
    {
        var query = _dbSet.AsQueryable();

        query = ApplyIncludes(query);
        query = ApplyFilter(query, filterOn, filterQuery);
        query = ApplySorting(query, sortBy, isAscending);

        // Pagination
        var skipAmount = (pageNumber - 1) * pageSize;
        if (skipAmount < 0) skipAmount = 0; // Ensure skipAmount is not negative

        query = query.Skip(skipAmount).Take(pageSize);

        return await query.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public virtual async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public virtual void Update(T entity)
    {
        _dbSet.Attach(entity);
        _context.Entry(entity).State = EntityState.Modified;
    }

    public virtual void Remove(T entity)
    {
        if (_context.Entry(entity).State == EntityState.Detached)
        {
            _dbSet.Attach(entity);
        }
        _dbSet.Remove(entity);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
    
    // Abstract methods to be implemented by derived repository classes
    protected abstract IQueryable<T> ApplyIncludes(IQueryable<T> query);
    protected abstract IQueryable<T> ApplyFilter(IQueryable<T> query, string? filterOn, string? filterQuery);
    protected abstract IQueryable<T> ApplySorting(IQueryable<T> query, string? sortBy, bool isAscending);
}