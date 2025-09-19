using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eventiq.Infrastructure.Persistence;

public class EventRepository:GenericRepository<Event>, IEventRepository
{
    public EventRepository(ApplicationDbContext context, ILogger<Event> logger) : base(context, logger)
    {
    }

    public async Task<int> GetOrgEventCountAsync(Guid orgId)
    {
        return await _dbSet
            .Where(e => e.OrganizationId == orgId 
                        && !e.IsDeleted 
                        && e.Status != EventStatus.Draft)
            .CountAsync();

    }
}