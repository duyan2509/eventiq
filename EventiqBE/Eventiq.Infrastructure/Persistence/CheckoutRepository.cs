using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eventiq.Infrastructure.Persistence;

public class CheckoutRepository : GenericRepository<Checkout>, ICheckoutRepository
{
    public CheckoutRepository(ApplicationDbContext context, ILogger<Checkout> logger) 
        : base(context, logger)
    {
    }

    public async Task<Checkout?> GetByCheckoutIdWithEventItemAsync(Guid checkoutId)
    {
        return await _dbSet
            .Include(c => c.EventItem)
            .ThenInclude(ei => ei!.Event)
            .FirstOrDefaultAsync(c => c.Id == checkoutId && !c.IsDeleted);
    }

    public async Task<IEnumerable<Checkout>> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Include(c => c.EventItem)
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Checkout>> GetByEventItemIdAsync(Guid eventItemId)
    {
        return await _dbSet
            .Include(c => c.EventItem)
            .Where(c => c.EventItemId == eventItemId && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }
}

