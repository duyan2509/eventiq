using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Repositories;

public interface IEventSeatRepository : IGenericRepository<EventSeat>
{
    Task<IEnumerable<EventSeat>> GetByChartIdAsync(Guid chartId);
    Task<EventSeat?> GetByChartIdAndSeatKeyAsync(Guid chartId, string seatKey);
    Task<bool> BulkUpsertAsync(IEnumerable<EventSeat> seats);
}

