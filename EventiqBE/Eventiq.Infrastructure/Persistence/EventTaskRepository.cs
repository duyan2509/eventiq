using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eventiq.Infrastructure.Persistence;

public class EventTaskRepository : GenericRepository<EventTask>, IEventTaskRepository
{
    public EventTaskRepository(ApplicationDbContext context, ILogger<EventTask> logger) : base(context, logger)
    {
    }

    public async Task<IEnumerable<EventTask>> GetByEventIdAsync(Guid eventId)
    {
        return await _dbSet
            .Include(t => t.Options)
            .Include(t => t.StaffAssignments)
                .ThenInclude(sa => sa.Staff)
            .Where(t => t.EventId == eventId)
            .ToListAsync();
    }

    public async Task<EventTask?> GetByIdWithAssignmentsAsync(Guid taskId)
    {
        return await _dbSet
            .Include(t => t.Options)
            .Include(t => t.StaffAssignments)
                .ThenInclude(sa => sa.Staff)
            .FirstOrDefaultAsync(t => t.Id == taskId);
    }
    
    public async Task<bool> HasAssignmentsAsync(Guid taskId)
    {
        return await _dbSet
            .Where(t => t.Id == taskId)
            .SelectMany(t => t.StaffAssignments)
            .AnyAsync();
    }
}

