using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eventiq.Infrastructure.Persistence;

public class EventApprovalHistoryRepository : GenericRepository<EventApprovalHistory>, IEventApprovalHistoryRepository
{
    public EventApprovalHistoryRepository(ApplicationDbContext context, ILogger<EventApprovalHistory> logger) 
        : base(context, logger)
    {
    }

    public async Task<IEnumerable<EventApprovalHistory>> GetByEventIdAsync(Guid eventId)
    {
        return await _dbSet
            .Where(h => h.EventId == eventId)
            .OrderByDescending(h => h.ActionDate)
            .ToListAsync();
    }
}

