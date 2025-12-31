using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Repositories;

public interface ITicketRepository : IGenericRepository<Ticket>
{
    Task<IEnumerable<Ticket>> GetByUserIdAsync(string userId);
    Task<IEnumerable<Ticket>> GetByEventItemIdAsync(Guid eventItemId);
    Task<IEnumerable<TicketClass>> GetTicketClassesByEventItemIdAsync(Guid eventItemId);
}

