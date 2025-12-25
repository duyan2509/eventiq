using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eventiq.Infrastructure.Persistence;

public class StaffRepository : GenericRepository<Staff>, IStaffRepository
{
    public StaffRepository(ApplicationDbContext context, ILogger<Staff> logger) : base(context, logger)
    {
    }

    public async Task<IEnumerable<Staff>> GetByEventIdAsync(Guid eventId)
    {
        return await _dbSet
            .Include(s => s.TaskAssignments)
                .ThenInclude(ta => ta.Task)
            .Include(s => s.TaskAssignments)
                .ThenInclude(ta => ta.Option)
            .Where(s => s.EventId == eventId)
            .ToListAsync();
    }

    public async Task<Staff?> GetByEventIdAndUserIdAsync(Guid eventId, Guid userId)
    {
        return await _dbSet
            .Include(s => s.TaskAssignments)
                .ThenInclude(ta => ta.Task)
            .Include(s => s.TaskAssignments)
                .ThenInclude(ta => ta.Option)
            .FirstOrDefaultAsync(s => s.EventId == eventId && s.UserId == userId);
    }

    public async Task<IEnumerable<Staff>> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Include(s => s.Event)
            .Where(s => s.UserId == userId)
            .ToListAsync();
    }

    public async Task<bool> HasOverlappingScheduleAsync(Guid userId, DateTime startTime, DateTime endTime, Guid? excludeEventId = null)
    {
        var query = _dbSet
            .Where(s => s.UserId == userId &&
                       s.StartTime < endTime &&
                       s.EndTime > startTime);

        if (excludeEventId.HasValue)
        {
            query = query.Where(s => s.EventId != excludeEventId.Value);
        }

        return await query.AnyAsync();
    }
}

