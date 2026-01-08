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

    public async Task<PaginatedResult<Event>> GetAllAsync(int page, int size, EventStatus? status = null)
    {
        var query = _dbSet
            .Include(e => e.Organization)
            .Include(e => e.EventAddress)
            .AsQueryable();
        
        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }
        
        var totalCount = await query.CountAsync();
        var events = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();
        
        return new PaginatedResult<Event>
        {
            Data = events,
            Total = totalCount,
            Page = page,
            Size = size
        };
    }

    public async Task<PaginatedResult<Event>> GetEventsAsync(string? search = null, int page = 1, int size = 10, string? timeSort = null, string? province = null, string? eventType = null)
    {
        var now = DateTime.UtcNow;
        
        var query = _dbSet
            .Include(e => e.Organization)
            .Include(e => e.EventAddress)
            .Include(e => e.EventItem)
            .Where(e => e.Status == EventStatus.Published && !e.IsDeleted);
        
        if (!string.IsNullOrWhiteSpace(eventType) && eventType.ToLower() != "all")
        {
            if (eventType.ToLower() == "upcoming")
            {
                query = query.Where(e => e.EventItem.Any(ei => ei.Start >= now));
            }
            else if (eventType.ToLower() == "past")
            {
                query = query.Where(e => e.EventItem.All(ei => ei.Start < now));
            }
        }
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(e => 
                e.Name.ToLower().Contains(searchLower) ||
                (e.Description != null && e.Description.ToLower().Contains(searchLower)) ||
                (e.Organization != null && e.Organization.Name.ToLower().Contains(searchLower)));
        }
        
        if (!string.IsNullOrWhiteSpace(province) && province.ToLower() != "all")
        {
            var provinceLower = province.ToLower();
            query = query.Where(e => 
                e.EventAddress != null && (
                    e.EventAddress.ProvinceCode.ToLower() == provinceLower ||
                    e.EventAddress.ProvinceName.ToLower().Contains(provinceLower)));
        }
        
        var totalCount = await query.CountAsync();
        
        IQueryable<Event> orderedQuery;
        if (timeSort?.ToLower() == "desc")
        {
            orderedQuery = query.OrderByDescending(e => e.EventItem.Min(ei => ei.Start));
        }
        else
        {
            orderedQuery = query.OrderBy(e => e.EventItem.Min(ei => ei.Start));
        }
        
        var events = await orderedQuery
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();
        
        return new PaginatedResult<Event>
        {
            Data = events,
            Total = totalCount,
            Page = page,
            Size = size
        };
    }
}