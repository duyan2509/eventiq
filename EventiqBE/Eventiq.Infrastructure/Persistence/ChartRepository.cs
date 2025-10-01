using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eventiq.Infrastructure.Persistence;

public class ChartRepository:GenericRepository<Chart>, IChartRepository
{
    public ChartRepository(ApplicationDbContext context, ILogger<Chart> logger) : base(context, logger)
    {
    }

    public async Task<IEnumerable<Chart>> GetByEventItAsync(Guid eventId)
    {
        return await _dbSet
            .Where(c => c.EventId == eventId)
            .ToListAsync();
    }

    public async Task<Chart?> GetDetailChartByIdAsync(Guid chartId)
    {
        return await _dbSet
            .Include(c=>c.EventItems)
            .FirstOrDefaultAsync(c=>c.Id == chartId);
    }
}