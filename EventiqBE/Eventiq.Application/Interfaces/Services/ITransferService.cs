using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces;

namespace Eventiq.Application.Interfaces.Services;

public interface ITransferService
{
    Task<string> TransferTicketAsync(Guid senderUserId, Guid ticketId, TransferTicketRequestDto request);
    Task<PaginatedResult<TransferRequestDto>> GetIncomingTransfersAsync(string receiverUserId, int page = 1, int size = 10);
    Task AcceptTransferAsync(Guid receiverUserId, Guid transferId);
    Task RejectTransferAsync(Guid receiverUserId, Guid transferId);
}
