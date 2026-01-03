using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Eventiq.Api.Hubs;

namespace Eventiq.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController : BaseController
{
    private readonly ICheckinService _checkinService;
    private readonly ITransferService _transferService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public TicketsController(ICheckinService checkinService, ITransferService transferService, IHubContext<NotificationHub> hubContext)
    {
        _checkinService = checkinService;
        _transferService = transferService;
        _hubContext = hubContext;
    }

    [HttpPost("{ticketId}/checkin-request")]
    public async Task<ActionResult<CheckInRequestResponseDto>> RequestCheckIn(
        [FromRoute] Guid ticketId,
        [FromBody] CheckInRequestDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _checkinService.RequestCheckInAsync(userId, ticketId, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{ticketId}/transfer")]
    public async Task<ActionResult> TransferTicket(
        [FromRoute] Guid ticketId,
        [FromBody] TransferTicketRequestDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var receiverId = await _transferService.TransferTicketAsync(userId, ticketId, request);
            
            if (Guid.TryParse(receiverId, out var receiverUserId))
            {
                await _hubContext.Clients.Group($"user_{receiverUserId}").SendAsync("TransferRequestReceived", new
                {
                    TicketId = ticketId,
                    Message = "You have received a new transfer request"
                });
            }
            
            return Ok(new { message = "Transfer request created successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("transfers/incoming")]
    public async Task<ActionResult<PaginatedResult<TransferRequestDto>>> GetIncomingTransfers([FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        try
        {
            var userId = GetCurrentUserId();
            var transfers = await _transferService.GetIncomingTransfersAsync(userId.ToString(), page, size);
            return Ok(transfers);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("transfers/{transferId}/accept")]
    public async Task<ActionResult> AcceptTransfer([FromRoute] Guid transferId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _transferService.AcceptTransferAsync(userId, transferId);
            return Ok(new { message = "Transfer accepted successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("transfers/{transferId}/reject")]
    public async Task<ActionResult> RejectTransfer([FromRoute] Guid transferId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _transferService.RejectTransferAsync(userId, transferId);
            return Ok(new { message = "Transfer rejected successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
