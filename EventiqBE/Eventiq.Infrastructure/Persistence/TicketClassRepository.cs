using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eventiq.Infrastructure.Persistence;

public class TicketClassRepository:GenericRepository<TicketClass>, ITicketClassRepository
{
    public TicketClassRepository(ApplicationDbContext context, ILogger<TicketClass> logger) : base(context, logger)
    {
    }

    public async Task<IEnumerable<TicketClass>> GetEventTicketClassesAsync(Guid eventId)
    {
        return await _dbSet.Where(tc => tc.EventId == eventId).ToListAsync();
    }
}