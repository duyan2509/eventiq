using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Repositories;

public interface IVerifyRequestRepository : IGenericRepository<VerifyRequest>
{
    Task<VerifyRequest?> GetByTicketIdAndStatusAsync(Guid ticketId, VerifyRequestStatus status);
    Task<VerifyRequest?> GetByTicketIdAsync(Guid ticketId);
}
