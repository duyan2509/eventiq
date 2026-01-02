using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eventiq.Infrastructure.Persistence;

public class CheckinRepository : GenericRepository<Checkin>, ICheckinRepository
{
    public CheckinRepository(ApplicationDbContext context, ILogger<Checkin> logger) : base(context, logger)
    {
    }

    public async Task<IEnumerable<Checkin>> GetByEventItemIdAsync(Guid eventItemId)
    {
        return await _dbSet
            .Include(c => c.Ticket)
            .Include(c => c.EventItem)
            .Include(c => c.Staff)
            .Where(c => c.EventItemId == eventItemId)
            .ToListAsync();
    }

    public async Task<PaginatedResult<Checkin>> GetByEventItemIdPaginatedAsync(Guid eventItemId, int page, int size)
    {
        var query = _dbSet
            .Include(c => c.Ticket)
            .Include(c => c.EventItem)
            .Include(c => c.Staff)
            .Where(c => c.EventItemId == eventItemId);

        var totalCount = await query.CountAsync();
        var checkins = await query
            .OrderByDescending(c => c.CheckinTime)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return new PaginatedResult<Checkin>
        {
            Data = checkins,
            Total = totalCount,
            Page = page,
            Size = size
        };
    }
}
