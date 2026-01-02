using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eventiq.Infrastructure.Persistence;

public class VerifyRequestRepository : GenericRepository<VerifyRequest>, IVerifyRequestRepository
{
    public VerifyRequestRepository(ApplicationDbContext context, ILogger<VerifyRequest> logger) 
        : base(context, logger)
    {
    }

    public async Task<VerifyRequest?> GetByTicketIdAndStatusAsync(Guid ticketId, VerifyRequestStatus status)
    {
        return await _dbSet
            .Include(vr => vr.Ticket)
            .Where(vr => vr.TicketId == ticketId && vr.Status == status && !vr.IsDeleted)
            .OrderByDescending(vr => vr.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<VerifyRequest?> GetByTicketIdAsync(Guid ticketId)
    {
        return await _dbSet
            .Include(vr => vr.Ticket)
            .Where(vr => vr.TicketId == ticketId && !vr.IsDeleted)
            .OrderByDescending(vr => vr.CreatedAt)
            .FirstOrDefaultAsync();
    }
}
