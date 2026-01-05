using Eventiq.Api.Request;
using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Application.Interfaces.Services;
using Eventiq.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Eventiq.Api.Hubs;

namespace Eventiq.Api.Controllers;

[Authorize]
public class AdminController : BaseController
{
    private readonly IEventService _eventService;
    private readonly IUserService _userService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ITransferRequestRepository _transferRequestRepository;

    public AdminController(
        IEventService eventService, 
        IUserService userService, 
        IHubContext<NotificationHub> hubContext,
        ITransferRequestRepository transferRequestRepository)
    {
        _eventService = eventService;
        _userService = userService;
        _hubContext = hubContext;
        _transferRequestRepository = transferRequestRepository;
    }

    [HttpGet("events")]
    public async Task<ActionResult<PaginatedResult<EventPreview>>> GetEvents(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] EventStatus? status = null)
    {
        try
        {
            var response = await _eventService.GetAllEventsAsync(page, size, status);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("events/{eventId}/approve")]
    public async Task<ActionResult<EventSubmissionResponseDto>> ApproveEvent(
        [FromRoute] Guid eventId,
        [FromBody] ApproveEventRequest? request)
    {
        try
        {
            var adminUserId = GetCurrentUserId();
            var comment = request?.Comment;
            var response = await _eventService.ApproveEventAsync(adminUserId, eventId, comment);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("events/{eventId}/reject")]
    public async Task<ActionResult<EventSubmissionResponseDto>> RejectEvent(
        [FromRoute] Guid eventId,
        [FromBody] RejectEventRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Comment))
                return BadRequest(new { message = "Rejection comment is required" });

            var adminUserId = GetCurrentUserId();
            var response = await _eventService.RejectEventAsync(adminUserId, eventId, request.Comment);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("events/{eventId}/approval-history")]
    public async Task<ActionResult<IEnumerable<EventApprovalHistoryDto>>> GetApprovalHistory(
        [FromRoute] Guid eventId)
    {
        try
        {
            var response = await _eventService.GetApprovalHistoryForAdminAsync(eventId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("users")]
    public async Task<ActionResult<PaginatedResult<AdminUserDto>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string? emailSearch = null)
    {
        try
        {
            var response = await _userService.GetUsersAsync(page, size, emailSearch);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("users/{userId}/ban")]
    public async Task<ActionResult> BanUser(
        [FromRoute] Guid userId,
        [FromBody] BanUserRequest? request)
    {
        try
        {
            var adminUserId = GetCurrentUserId();
            await _userService.BanUserAsync(userId, adminUserId, request?.BanReason);

            var pendingTransfers = await _transferRequestRepository.GetAllAsync();
            var userPendingTransfers = pendingTransfers
                .Where(tr => tr.ToUserId == userId.ToString() && tr.Status == TransferRequestStatus.PENDING)
                .ToList();

            foreach (var transfer in userPendingTransfers)
            {
                await _hubContext.Clients.User(transfer.SenderUserId).SendAsync("TransferRequestCancelled", new
                {
                    TransferRequestId = transfer.Id,
                    Reason = "User has been banned"
                });
            }

            await _hubContext.Clients.User(userId.ToString()).SendAsync("UserBanned", new
            {
                UserId = userId,
                Reason = request?.BanReason
            });

            return Ok(new { message = "User banned successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("users/{userId}/unban")]
    public async Task<ActionResult> UnbanUser([FromRoute] Guid userId)
    {
        try
        {
            var adminUserId = GetCurrentUserId();
            await _userService.UnbanUserAsync(userId, adminUserId);

            await _hubContext.Clients.User(userId.ToString()).SendAsync("UserUnbanned", new
            {
                UserId = userId
            });

            return Ok(new { message = "User unbanned successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

