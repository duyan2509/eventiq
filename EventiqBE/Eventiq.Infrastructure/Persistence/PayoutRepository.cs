using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eventiq.Infrastructure.Persistence;

public class PayoutRepository : GenericRepository<Payout>, IPayoutRepository
{
    public PayoutRepository(ApplicationDbContext context, ILogger<Payout> logger) 
        : base(context, logger)
    {
    }

    public async Task<Payout?> GetByEventItemIdAsync(Guid eventItemId)
    {
        return await _dbSet
            .Include(p => p.Event)
            .Include(p => p.EventItem)
            .Include(p => p.Organization)
            .FirstOrDefaultAsync(p => p.EventItemId == eventItemId && !p.IsDeleted);
    }

    public async Task<IEnumerable<Payout>> GetByEventIdAsync(Guid eventId)
    {
        return await _dbSet
            .Include(p => p.Event)
            .Include(p => p.EventItem)
            .Include(p => p.Organization)
            .Where(p => p.EventId == eventId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Payout>> GetByOrganizationIdAsync(Guid organizationId)
    {
        return await _dbSet
            .Include(p => p.Event)
            .Include(p => p.EventItem)
            .Include(p => p.Organization)
            .Where(p => p.OrganizationId == organizationId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Payout>> GetPendingPayoutsAsync()
    {
        return await _dbSet
            .Include(p => p.Event)
            .Include(p => p.EventItem)
            .Include(p => p.Organization)
            .Where(p => p.Status == PayoutStatus.PENDING && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Payout>> GetPaidPayoutsAsync()
    {
        return await _dbSet
            .Include(p => p.Event)
            .Include(p => p.EventItem)
            .Include(p => p.Organization)
            .Where(p => p.Status == PayoutStatus.PAID && !p.IsDeleted)
            .OrderByDescending(p => p.PaidAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Payout>> GetPayoutsByFiltersAsync(PayoutStatus? status, int? month, int? year)
    {
        var query = _dbSet
            .Include(p => p.Event)
            .Include(p => p.EventItem)
            .Include(p => p.Organization)
            .Where(p => !p.IsDeleted);

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        if (month.HasValue && year.HasValue)
        {
            var startDate = new DateTime(year.Value, month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = startDate.AddMonths(1).AddMilliseconds(-1);
            
            query = query.Where(p => 
                (p.Status == PayoutStatus.PAID && p.PaidAt.HasValue && p.PaidAt.Value >= startDate && p.PaidAt.Value <= endDate) ||
                (p.Status == PayoutStatus.PENDING && p.CreatedAt >= startDate && p.CreatedAt <= endDate)
            );
        }

        return await query
            .OrderByDescending(p => p.Status == PayoutStatus.PENDING ? p.CreatedAt : (p.PaidAt ?? p.CreatedAt))
            .ToListAsync();
    }
}

