using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Repositories;

public interface IEventItemRepository:IGenericRepository<EventItem>
{
    Task<IEnumerable<EventItem>> GetAllByEventIdAsync(Guid eventId);
    Task<EventItem?> GetByDetailByIdAsync(Guid id);
}