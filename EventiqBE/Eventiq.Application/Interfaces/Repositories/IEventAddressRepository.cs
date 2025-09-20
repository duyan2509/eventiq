using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Repositories;

public interface IEventAddressRepository:IGenericRepository<EventAddress>
{
    Task<EventAddress?> GetByEventIdAsync(Guid eventId);
}