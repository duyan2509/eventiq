using Eventiq.Application.Dtos;
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

    public async Task<PaginatedResult<Event>> GetByOrgAsync(Guid orgId, int page, int size)
    {
        var query =  _dbSet
            .Include(e => e.Organization)
            .Include(e=>e.EventAddress)
            .Where(e => e.OrganizationId == orgId);
        var totalCount = await query.CountAsync();
        var evnt = await query
            .OrderBy(e => e.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();
        return new PaginatedResult<Event>
        {
            Data = evnt,
            Total = totalCount,
            Page = page,
            Size = size
        };
    }

    public async Task<Event?> GetDetailEventAsync(Guid eventId)
    {
        return await _dbSet
            .Include(e => e.EventAddress)
            .Include(e => e.Organization)
            .FirstOrDefaultAsync(e => e.Id == eventId);
    }
}