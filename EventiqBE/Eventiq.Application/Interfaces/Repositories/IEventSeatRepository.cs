using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Repositories;

public interface IEventSeatRepository : IGenericRepository<EventSeat>
{
    Task<IEnumerable<EventSeat>> GetByChartIdAsync(Guid chartId);
    Task<EventSeat?> GetByChartIdAndSeatKeyAsync(Guid chartId, string seatKey);
    Task<bool> BulkUpsertAsync(IEnumerable<EventSeat> seats);
    Task<IEnumerable<EventSeat>> GetSeatsByKeysAsync(Guid chartId, List<string> seatKeys);
    Task<EventSeat?> GetSeatByKeyAsync(Guid chartId, string seatKey);
    Task<IEnumerable<EventSeat>> GetSeatsByLabelsAsync(Guid chartId, List<string> labels);
    Task<EventSeat?> GetSeatByLabelAsync(Guid chartId, string label);
}

