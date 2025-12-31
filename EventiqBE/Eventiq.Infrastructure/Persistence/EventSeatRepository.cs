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
        return await GetSeatByLabelAsync(chartId, seatKey);
    }

    public async Task<bool> BulkUpsertAsync(IEnumerable<EventSeat> seats)
    {
        try
        {
            foreach (var seat in seats)
            {
                var existing = await GetSeatByLabelAsync(seat.ChartId, seat.Label);
                if (existing != null)
                {
                    existing.Section = seat.Section;
                    existing.Row = seat.Row;
                    existing.Number = seat.Number;
                    existing.CategoryKey = seat.CategoryKey;
                    existing.ExtraData = seat.ExtraData;
                    _dbSet.Update(existing);
                }
                else
                {
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

    public async Task<IEnumerable<EventSeat>> GetSeatsByKeysAsync(Guid chartId, List<string> seatKeys)
    {
        return await GetSeatsByLabelsAsync(chartId, seatKeys);
    }

    public async Task<EventSeat?> GetSeatByKeyAsync(Guid chartId, string seatKey)
    {
        return await GetSeatByLabelAsync(chartId, seatKey);
    }

    public async Task<IEnumerable<EventSeat>> GetSeatsByLabelsAsync(Guid chartId, List<string> labels)
    {
        return await _dbSet
            .Where(s => s.ChartId == chartId && labels.Contains(s.Label))
            .ToListAsync();
    }

    public async Task<EventSeat?> GetSeatByLabelAsync(Guid chartId, string label)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.ChartId == chartId && s.Label == label);
    }
}

