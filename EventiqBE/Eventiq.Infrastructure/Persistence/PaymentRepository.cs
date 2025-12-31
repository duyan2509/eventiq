using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eventiq.Infrastructure.Persistence;

public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(ApplicationDbContext context, ILogger<Payment> logger) 
        : base(context, logger)
    {
    }

    public async Task<Payment?> GetByPaymentIdAsync(string paymentId)
    {
        return await _dbSet
            .Include(p => p.Checkout)
            .Include(p => p.EventItem)
            .ThenInclude(ei => ei.Event)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId && !p.IsDeleted);
    }

    public async Task<Payment?> GetByCheckoutIdAsync(Guid checkoutId)
    {
        return await _dbSet
            .Include(p => p.Checkout)
            .Include(p => p.EventItem)
            .ThenInclude(ei => ei.Event)
            .FirstOrDefaultAsync(p => p.CheckoutId == checkoutId && !p.IsDeleted);
    }

    public async Task<IEnumerable<Payment>> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Include(p => p.Checkout)
            .Include(p => p.EventItem)
            .ThenInclude(ei => ei.Event)
            .Where(p => p.UserId == userId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Payment>> GetByEventItemIdAsync(Guid eventItemId)
    {
        return await _dbSet
            .Include(p => p.Checkout)
            .Include(p => p.EventItem)
            .ThenInclude(ei => ei.Event)
            .Where(p => p.EventItemId == eventItemId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Payment>> GetSuccessfulPaymentsByEventItemIdAsync(Guid eventItemId)
    {
        return await _dbSet
            .Include(p => p.Checkout)
            .Include(p => p.EventItem)
            .ThenInclude(ei => ei.Event)
            .Where(p => p.EventItemId == eventItemId && 
                       p.Status == PaymentStatus.SUCCESS && 
                       !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
}

