using Eventiq.Application.Dtos;
using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Repositories;

public interface ITicketClassRepository:IGenericRepository<TicketClass>
{
    Task<IEnumerable<TicketClass>> GetEventTicketClassesAsync(Guid eventId);
}