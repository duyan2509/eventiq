using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Repositories;

public interface ITaskOptionRepository : IGenericRepository<TaskOption>
{
    Task<IEnumerable<TaskOption>> GetByTaskIdAsync(Guid taskId);
    Task<TaskOption?> GetByIdWithAssignmentsAsync(Guid optionId);
    Task<bool> HasAssignmentsAsync(Guid optionId);
}

