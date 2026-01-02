using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eventiq.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CheckinController : ControllerBase
{
    private readonly ICheckinService _checkinService;

    public CheckinController(ICheckinService checkinService)
    {
        _checkinService = checkinService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            throw new UnauthorizedAccessException("Invalid user token");
        return userId;
    }

    [HttpGet("events/{eventId}/items/{eventItemId}")]
    public async Task<ActionResult<PaginatedResult<CheckinDto>>> GetCheckins(
        [FromRoute] Guid eventId,
        [FromRoute] Guid eventItemId,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _checkinService.GetCheckinsAsync(userId, eventId, eventItemId, page, size);
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

    [HttpPost("items/{eventItemId}/checkin")]
    public async Task<ActionResult<CheckinDto>> CheckinTicket(
        [FromRoute] Guid eventItemId,
        [FromBody] CheckinTicketRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _checkinService.CheckinTicketAsync(userId, eventItemId, request);
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
