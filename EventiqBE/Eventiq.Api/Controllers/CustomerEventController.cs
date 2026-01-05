using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eventiq.Api.Controllers;

[Route("api/events")]
public class CustomerEventController : BaseController
{
    private readonly IEventService _eventService;

    public CustomerEventController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PaginatedResult<CustomerEventDto>>> GetEvents(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string? timeSort = null,
        [FromQuery] string? province = null,
        [FromQuery] string? eventType = null)
    {
        try
        {
            var result = await _eventService.GetEventsAsync(search, page, size, timeSort, province, eventType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get published event detail (public endpoint)
    /// </summary>
    [HttpGet("{eventId}")]
    [AllowAnonymous]
    public async Task<ActionResult<CustomerEventDetailDto>> GetPublishedEventDetail([FromRoute] Guid eventId)
    {
        try
        {
            var result = await _eventService.GetPublishedEventDetailAsync(eventId);
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

    /// <summary>
    /// Get seat map for event item (public endpoint)
    /// </summary>
    [HttpGet("{eventId}/items/{eventItemId}/seat-map")]
    [AllowAnonymous]
    public async Task<ActionResult<CustomerSeatMapDto>> GetEventItemSeatMap(
        [FromRoute] Guid eventId,
        [FromRoute] Guid eventItemId)
    {
        try
        {
            var result = await _eventService.GetEventItemSeatMapAsync(eventId, eventItemId);
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

