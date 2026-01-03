using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Eventiq.Application.Dtos;

namespace Eventiq.Infrastructure.Persistence;

public class TransferRequestRepository : GenericRepository<TransferRequest>, ITransferRequestRepository
{
    public TransferRequestRepository(ApplicationDbContext context, ILogger<TransferRequest> logger) 
        : base(context, logger)
    {
    }

    public async Task<TransferRequest?> GetByTicketIdAndStatusAsync(Guid ticketId, TransferRequestStatus status)
    {
        return await _dbSet
            .Include(tr => tr.Ticket)
            .FirstOrDefaultAsync(tr => tr.TicketId == ticketId && tr.Status == status && !tr.IsDeleted);
    }

    public async Task<PaginatedResult<TransferRequest>> GetIncomingTransfersByUserIdPaginatedAsync(string userId, int page = 1, int size = 10)
    {
        var query = _dbSet
            .Include(tr => tr.Ticket)
                .ThenInclude(t => t.EventItem)
                    .ThenInclude(ei => ei.Event)
            .Where(tr => tr.ToUserId == userId && !tr.IsDeleted)
            .OrderByDescending(tr => tr.CreatedAt);

        var total = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return new PaginatedResult<TransferRequest>
        {
            Data = data,
            Total = total,
            Page = page,
            Size = size
        };
    }

    public async Task<List<TransferRequest>> GetPendingTransfersByTicketIdAsync(Guid ticketId)
    {
        return await _dbSet
            .Where(tr => tr.TicketId == ticketId && tr.Status == TransferRequestStatus.PENDING && !tr.IsDeleted)
            .ToListAsync();
    }
}
