using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces;
using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Application.Interfaces.Services;
using Eventiq.Domain.Entities;

namespace Eventiq.Application.Services;

public class TransferService : ITransferService
{
    private readonly ITransferRequestRepository _transferRequestRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventItemRepository _eventItemRepository;

    public TransferService(
        ITransferRequestRepository transferRequestRepository,
        ITicketRepository ticketRepository,
        IIdentityService identityService,
        IUnitOfWork unitOfWork,
        IEventItemRepository eventItemRepository)
    {
        _transferRequestRepository = transferRequestRepository;
        _ticketRepository = ticketRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
        _eventItemRepository = eventItemRepository;
    }

    public async Task<string> TransferTicketAsync(Guid senderUserId, Guid ticketId, TransferTicketRequestDto request)
    {
        var isBanned = await _identityService.IsUserBannedAsync(senderUserId);
        if (isBanned)
            throw new UnauthorizedAccessException("Your account has been banned. You cannot transfer tickets.");

        var isPasswordValid = await _identityService.VerifyPasswordAsync(senderUserId, request.Password);
        if (!isPasswordValid)
            throw new UnauthorizedAccessException("Invalid password");

        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        if (ticket == null || ticket.IsDeleted)
            throw new KeyNotFoundException("Ticket not found");

        if (ticket.UserId != senderUserId.ToString())
            throw new UnauthorizedAccessException("Ticket does not belong to this user");

        if (ticket.Status == TicketStatus.USED)
            throw new InvalidOperationException("Cannot transfer used ticket");

        var eventItem = await _eventItemRepository.GetByDetailByIdAsync(ticket.EventItemId);
        if (eventItem == null)
            throw new KeyNotFoundException("Event item not found");

        if (eventItem.End < DateTime.UtcNow)
            throw new InvalidOperationException("Cannot transfer expired ticket");

        var existingPendingRequest = await _transferRequestRepository.GetByTicketIdAndStatusAsync(ticketId, TransferRequestStatus.PENDING);
        if (existingPendingRequest != null)
            throw new InvalidOperationException("Ticket already has a pending transfer request");

        var receiver = await _identityService.GetByEmailAsync(request.ToUserEmail);
        if (receiver == null)
            throw new KeyNotFoundException("Receiver user not found");

        if (receiver.Id == senderUserId.ToString())
            throw new InvalidOperationException("Cannot transfer ticket to yourself");

        var transferRequest = new TransferRequest
        {
            TicketId = ticketId,
            SenderUserId = senderUserId.ToString(),
            ToUserId = receiver.Id,
            Status = TransferRequestStatus.PENDING,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        await _transferRequestRepository.AddAsync(transferRequest);
        
        return receiver.Id;
    }

    public async Task<PaginatedResult<TransferRequestDto>> GetIncomingTransfersAsync(string receiverUserId, int page = 1, int size = 10)
    {
        var paginatedTransfers = await _transferRequestRepository.GetIncomingTransfersByUserIdPaginatedAsync(receiverUserId, page, size);
        var transferDtos = new List<TransferRequestDto>();

        foreach (var transfer in paginatedTransfers.Data)
        {
            var ticket = transfer.Ticket;
            if (ticket == null) continue;

            var eventItem = ticket.EventItem;
            if (eventItem == null) continue;

            var eventEntity = eventItem.Event;
            if (eventEntity == null) continue;

            var sender = await _identityService.GetByIdAsync(Guid.Parse(transfer.SenderUserId));
            var receiver = await _identityService.GetByIdAsync(Guid.Parse(transfer.ToUserId));

            var senderName = !string.IsNullOrEmpty(sender?.Username) ? sender.Username : sender?.Email ?? string.Empty;
            var receiverName = !string.IsNullOrEmpty(receiver?.Username) ? receiver.Username : receiver?.Email ?? string.Empty;

            transferDtos.Add(new TransferRequestDto
            {
                Id = transfer.Id,
                TicketId = transfer.TicketId,
                SenderName = senderName,
                ReceiverName = receiverName,
                EventName = eventEntity.Name,
                EventDate = eventItem.Start,
                Status = transfer.Status.ToString(),
                ExpiresAt = transfer.ExpiresAt,
                CreatedAt = transfer.CreatedAt
            });
        }

        return new PaginatedResult<TransferRequestDto>
        {
            Data = transferDtos,
            Total = paginatedTransfers.Total,
            Page = paginatedTransfers.Page,
            Size = paginatedTransfers.Size
        };
    }

    public async Task AcceptTransferAsync(Guid receiverUserId, Guid transferId)
    {
        var isBanned = await _identityService.IsUserBannedAsync(receiverUserId);
        if (isBanned)
            throw new UnauthorizedAccessException("Your account has been banned. You cannot accept transfer requests.");

        var transfer = await _transferRequestRepository.GetByIdAsync(transferId);
        if (transfer == null || transfer.IsDeleted)
            throw new KeyNotFoundException("Transfer request not found");

        if (transfer.ToUserId != receiverUserId.ToString())
            throw new UnauthorizedAccessException("Transfer request does not belong to this user");

        if (transfer.Status != TransferRequestStatus.PENDING)
            throw new InvalidOperationException("Transfer request is not pending");

        if (transfer.ExpiresAt < DateTime.UtcNow)
        {
            transfer.Status = TransferRequestStatus.EXPIRED;
            await _transferRequestRepository.UpdateAsync(transfer);
            throw new InvalidOperationException("Transfer request has expired");
        }

        var ticket = await _ticketRepository.GetByIdAsync(transfer.TicketId);
        if (ticket == null || ticket.IsDeleted)
            throw new KeyNotFoundException("Ticket not found");

        if (ticket.Status == TicketStatus.USED)
            throw new InvalidOperationException("Ticket has already been used");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            ticket.UserId = receiverUserId.ToString();
            await _ticketRepository.UpdateAsync(ticket);

            transfer.Status = TransferRequestStatus.ACCEPTED;
            await _transferRequestRepository.UpdateAsync(transfer);

            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task RejectTransferAsync(Guid receiverUserId, Guid transferId)
    {
        var transfer = await _transferRequestRepository.GetByIdAsync(transferId);
        if (transfer == null || transfer.IsDeleted)
            throw new KeyNotFoundException("Transfer request not found");

        if (transfer.ToUserId != receiverUserId.ToString())
            throw new UnauthorizedAccessException("Transfer request does not belong to this user");

        if (transfer.Status != TransferRequestStatus.PENDING)
            throw new InvalidOperationException("Transfer request is not pending");

        transfer.Status = TransferRequestStatus.REJECTED;
        await _transferRequestRepository.UpdateAsync(transfer);
    }
}
