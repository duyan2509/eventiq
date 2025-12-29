using Eventiq.Api.Request;
using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces.Services;
using Eventiq.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eventiq.Api.Controllers;

[Authorize]
public class AdminController : BaseController
{
    private readonly IEventService _eventService;

    public AdminController(IEventService eventService)
    {
        _eventService = eventService;
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
            // Admin can view any event's approval history
            var response = await _eventService.GetApprovalHistoryForAdminAsync(eventId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

