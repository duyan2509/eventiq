using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eventiq.Infrastructure.Persistence;

public class EventAddressRepository:GenericRepository<EventAddress>, IEventAddressRepository
{
    public EventAddressRepository(ApplicationDbContext context, ILogger<EventAddress> logger) : base(context, logger)
    {
    }

    public async Task<EventAddress?> GetByEventIdAsync(Guid eventId)
    {
        return await _dbSet.SingleOrDefaultAsync(a => a.EventId == eventId);
    }
}