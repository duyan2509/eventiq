using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eventiq.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController : BaseController
{
    private readonly ICheckinService _checkinService;

    public TicketsController(ICheckinService checkinService)
    {
        _checkinService = checkinService;
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
