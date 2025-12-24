using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Repositories;

public interface IEventApprovalHistoryRepository : IGenericRepository<EventApprovalHistory>
{
    Task<IEnumerable<EventApprovalHistory>> GetByEventIdAsync(Guid eventId);
}

