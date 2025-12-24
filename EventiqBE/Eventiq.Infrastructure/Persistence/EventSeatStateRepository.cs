using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eventiq.Infrastructure.Persistence;

public class EventSeatStateRepository : GenericRepository<EventSeatState>, IEventSeatStateRepository
{
    public EventSeatStateRepository(ApplicationDbContext context, ILogger<EventSeatState> logger) 
        : base(context, logger)
    {
    }

    public async Task<IEnumerable<EventSeatState>> GetByEventItemIdAsync(Guid eventItemId)
    {
        return await _dbSet
            .Include(s => s.EventSeat)
            .Where(s => s.EventItemId == eventItemId)
            .ToListAsync();
    }

    public async Task<Dictionary<Guid, SeatStatus>> GetSeatStatusMapAsync(Guid eventItemId, IEnumerable<Guid> eventSeatIds)
    {
        var states = await _dbSet
            .Where(s => s.EventItemId == eventItemId && eventSeatIds.Contains(s.EventSeatId))
            .ToListAsync();
        
        return states.ToDictionary(s => s.EventSeatId, s => s.Status);
    }

    public async Task<EventSeatState?> GetByEventItemAndSeatAsync(Guid eventItemId, Guid eventSeatId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.EventItemId == eventItemId && s.EventSeatId == eventSeatId);
    }
}

