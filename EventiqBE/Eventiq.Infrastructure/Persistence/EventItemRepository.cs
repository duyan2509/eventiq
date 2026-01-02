using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Domain.Entities;
using Eventiq.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eventiq.Infrastructure.Identity;

public class EventItemRepository:GenericRepository<EventItem>, IEventItemRepository
{
    public EventItemRepository(ApplicationDbContext context, ILogger<EventItem> logger) : base(context, logger)
    {
    }


    public async Task<IEnumerable<EventItem>> GetAllByEventIdAsync(Guid eventId)
    {
        return await _dbSet
            .Include(e => e.Chart)
            .Include(e => e.Event)
            .ThenInclude(ev => ev.Organization)
            .Where(e => e.EventId == eventId)
            .ToListAsync();
    }

    public async Task<EventItem?> GetByDetailByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(e => e.Chart)
            .Include(e => e.Event)
            .ThenInclude(ev => ev.Organization)
            .FirstOrDefaultAsync(e => e.Id == id);
    }
}