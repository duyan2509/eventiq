using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces;
using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Repositories;

public interface ITransferRequestRepository : IGenericRepository<TransferRequest>
{
    Task<TransferRequest?> GetByTicketIdAndStatusAsync(Guid ticketId, TransferRequestStatus status);
    Task<PaginatedResult<TransferRequest>> GetIncomingTransfersByUserIdPaginatedAsync(string userId, int page = 1, int size = 10);
    Task<List<TransferRequest>> GetPendingTransfersByTicketIdAsync(Guid ticketId);
}
