using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Repositories;

public interface IEventTaskRepository : IGenericRepository<EventTask>
{
    Task<IEnumerable<EventTask>> GetByEventIdAsync(Guid eventId);
    Task<EventTask?> GetByIdWithAssignmentsAsync(Guid taskId);
    Task<bool> HasAssignmentsAsync(Guid taskId);
}

