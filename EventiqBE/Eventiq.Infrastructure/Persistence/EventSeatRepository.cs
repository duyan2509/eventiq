using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eventiq.Infrastructure.Persistence;

public class EventSeatRepository : GenericRepository<EventSeat>, IEventSeatRepository
{
    public EventSeatRepository(ApplicationDbContext context, ILogger<EventSeat> logger) 
        : base(context, logger)
    {
    }

    public async Task<IEnumerable<EventSeat>> GetByChartIdAsync(Guid chartId)
    {
        return await _dbSet
            .Where(s => s.ChartId == chartId)
            .ToListAsync();
    }

    public async Task<EventSeat?> GetByChartIdAndSeatKeyAsync(Guid chartId, string seatKey)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.ChartId == chartId && s.SeatKey == seatKey);
    }

    public async Task<bool> BulkUpsertAsync(IEnumerable<EventSeat> seats)
    {
        try
        {
            foreach (var seat in seats)
            {
                var existing = await GetByChartIdAndSeatKeyAsync(seat.ChartId, seat.SeatKey);
                if (existing != null)
                {
                    // Update existing
                    existing.Label = seat.Label;
                    existing.Section = seat.Section;
                    existing.Row = seat.Row;
                    existing.Number = seat.Number;
                    existing.CategoryKey = seat.CategoryKey;
                    existing.ExtraData = seat.ExtraData;
                    _dbSet.Update(existing);
                }
                else
                {
                    // Add new
                    await _dbSet.AddAsync(seat);
                }
            }
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk upsert seats");
            return false;
        }
    }
}

