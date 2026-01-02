using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eventiq.Infrastructure.Persistence;

public class TicketRepository : GenericRepository<Ticket>, ITicketRepository
{
    public TicketRepository(ApplicationDbContext context, ILogger<Ticket> logger) 
        : base(context, logger)
    {
    }

    public async Task<IEnumerable<Ticket>> GetByUserIdAsync(string userId)
    {
        return await _dbSet
            .Include(t => t.EventItem)
            .Include(t => t.TicketClass)
            .Where(t => t.UserId == userId && !t.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetByEventItemIdAsync(Guid eventItemId)
    {
        return await _dbSet
            .Include(t => t.TicketClass)
            .Where(t => t.EventItemId == eventItemId && !t.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<TicketClass>> GetTicketClassesByEventItemIdAsync(Guid eventItemId)
    {
        var eventItem = await _context.Set<EventItem>()
            .Include(ei => ei.Event)
            .ThenInclude(e => e.TicketClasses)
            .FirstOrDefaultAsync(ei => ei.Id == eventItemId && !ei.IsDeleted);
        
        if (eventItem?.Event == null)
        {
            return new List<TicketClass>();
        }
        
        return eventItem.Event.TicketClasses.Where(tc => !tc.IsDeleted).ToList();
    }

    public async Task<Ticket?> GetByTicketCodeAsync(string ticketCode)
    {
        return await _dbSet
            .Include(t => t.EventItem)
            .Include(t => t.TicketClass)
            .FirstOrDefaultAsync(t => t.TicketCode == ticketCode && !t.IsDeleted);
    }
}

