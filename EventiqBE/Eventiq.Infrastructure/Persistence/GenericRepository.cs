using System.Data;
using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Eventiq.Infrastructure.Persistence;

public class GenericRepository<T> : IGenericRepository<T>
    where T : BaseEntity 
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;
    protected readonly ILogger<T> _logger;

    public GenericRepository(ApplicationDbContext context, ILogger<T> logger)
    {
        _context = context;
        _dbSet = context.Set<T>();
        _logger = logger;
    }


    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FirstOrDefaultAsync(e => e.Id == id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }
    

    public virtual async Task<T> AddAsync(T entity)
    {
        try
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
        {
            throw new DuplicateNameException();
        }
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        try
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
        {
            throw new DuplicateNameException();
        }
    }

    public virtual async Task SoftDeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }


    public virtual async Task<bool> ExistsAsync(Guid id)
    {
        return await _dbSet.AnyAsync(e => e.Id == id);
    }

    public virtual async Task<int> CountAsync()
    {
        return await _dbSet.CountAsync();
    }

   
    public virtual async Task<T?> GetByIdIncludingDeletedAsync(Guid id)
    {
        return await _dbSet.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id);
    }

    public virtual async Task HardDeleteAsync(Guid id)
    {
        _logger.LogInformation("Hard Delete: {id}", id);
        try
        {
            var entity = await GetByIdAsync(id);
            if (entity == null)
            {
                _logger.LogWarning("Entity not found: {id}", id);
                return;
            }

            _logger.LogInformation("Removing entity: {id}, state: {state}", id, _context.Entry(entity).State);
            _dbSet.Remove(entity);

            var affected = await _context.SaveChangesAsync();
            _logger.LogInformation("Rows deleted: {affected}", affected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during hard delete for id {id}", id);
            throw;
        }
    }

    public virtual async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await _dbSet.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
    }

    public virtual async Task DeleteRangeAsync(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
        await _context.SaveChangesAsync();
    }
}
