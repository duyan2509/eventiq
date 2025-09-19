namespace Eventiq.Application.Interfaces.Repositories;

public interface IGenericRepository<T>
{
    Task<T?> GetByIdAsync(Guid id);
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<IEnumerable<T>> GetAllAsync();

    Task SoftDeleteAsync(Guid id);
    Task HardDeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<int> CountAsync();
    Task<T?> GetByIdIncludingDeletedAsync(Guid id);
    Task AddRangeAsync(IEnumerable<T> entities);
    Task DeleteRangeAsync(IEnumerable<T> entities);

}
