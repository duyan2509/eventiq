using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Repositories;

public interface IEventSeatStateRepository : IGenericRepository<EventSeatState>
{
    Task<IEnumerable<EventSeatState>> GetByEventItemIdAsync(Guid eventItemId);
    Task<Dictionary<Guid, SeatStatus>> GetSeatStatusMapAsync(Guid eventItemId, IEnumerable<Guid> eventSeatIds);
    Task<EventSeatState?> GetByEventItemAndSeatAsync(Guid eventItemId, Guid eventSeatId);
}

