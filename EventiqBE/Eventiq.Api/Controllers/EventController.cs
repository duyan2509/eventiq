using System.Data;
using AutoMapper;
using Eventiq.Api.Request;
using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Eventiq.Api.Controllers;

[Authorize]
public class EventController:BaseController
{
    protected readonly IMapper _mapper;

    public EventController(IMapper mapper, IEventService eventService, ISeatService seatService)
    {
        _mapper = mapper;
        _eventService = eventService;
        _seatService = seatService;
    }

    protected readonly IEventService _eventService;
    protected readonly ISeatService _seatService;
    [Authorize(Policy = "Event.Create")]
    [HttpPost]
    public async Task<ActionResult<CreateEventResponse>> PostEvent([FromForm] CreateEventRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        try
        {
            var userId = GetCurrentUserId();
            var dto = _mapper.Map<CreateEventDto>(request);
            {
                var memStream = new MemoryStream();
                await request.Banner.CopyToAsync(memStream);
                memStream.Position = 0;
                dto.BannerStream = memStream;
            }
            Console.WriteLine(dto.ToString());
            var response = await _eventService.CreateEventInfoAsync(userId, dto);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpGet("{eventId}")]
    public async Task<ActionResult<EventDetail>> GetEventById([FromRoute] Guid eventId)
    {
        try
        {
            var response = await _eventService.GetByIdAsync(eventId);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [Authorize(Policy = "Event.Update")]

    [HttpPatch("{eventId}/address")]
    public async Task<ActionResult<UpdateAddressResponse>> UpdateEventAddressAsync([FromRoute] Guid eventId, UpdateEventAddressDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _eventService.UpdateEventAddressAsync(userId, eventId, dto);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [Authorize(Policy = "Event.Update")]
    [HttpPatch("{eventId}")]
    public async Task<ActionResult<EventDto>> UpdateEventInfoAsync([FromRoute] Guid eventId, UpdateEventRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var dto = _mapper.Map<UpdateEventDto>(request);
            {
                var memStream = new MemoryStream();
                await request.Banner.CopyToAsync(memStream);
                memStream.Position = 0;
                dto.BannerStream = memStream;
            }
            var response = await _eventService.UpdateEventInfoAsync(userId, eventId, dto);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [Authorize(Policy = "Event.Update")]
    [HttpPut("{eventId}/payment")]
    public async Task<ActionResult<PaymentInformationResponse>> UpdateEventPaymentAsync([FromRoute] Guid eventId, UpdatePaymentInformation dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _eventService.UpdateEventPaymentAsync(userId, eventId, dto);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return  Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [Authorize(Policy = "Event.Update")]
    [HttpPost("{eventId}/ticket-class")]
    public async Task<ActionResult<TicketClassDto>> PostTicketClass([FromRoute]Guid eventId, CreateTicketClassDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _eventService.CreateTicketClassAsync(userId, eventId, dto);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [Authorize(Policy = "Event.Update")]
    [HttpPatch("{eventId}/ticket-class/{ticketClassId}")]
    public async Task<ActionResult<TicketClassDto>> PatchTicketClass([FromRoute]Guid eventId, [FromRoute]Guid ticketClassId,UpdateTicketClassInfoDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _eventService.UpdateTicketClassInfoAsync(userId, eventId,ticketClassId, dto);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpGet("{eventId}/ticket-class")]
    public async Task<ActionResult<IEnumerable<TicketClassDto>>> GetEventTicketClasses([FromRoute]Guid eventId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _eventService.GetEventTicketClassesAsync(userId, eventId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [Authorize(Policy = "Event.Create")]
    [HttpPost("{eventId}/event-item")]
    public async Task<ActionResult<EventItemDto>>PostEventItem([FromRoute]Guid eventId, [FromBody] CreateEventItemDto dto)
    {
        try
        {
            if (!ModelState.IsValid || !dto.CheckValidTime())
                return BadRequest(ModelState);
            var userId = GetCurrentUserId();
            var response = await _eventService.CreateEventItemAsync(userId, eventId, dto);
            return Ok(response);
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
    [HttpGet("{eventId}/event-item")]
    public async Task<ActionResult<IEnumerable<EventItemDto>>> GetEventItems([FromRoute]Guid eventId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _eventService.GetEventItemAsync(eventId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [Authorize(Policy = "Event.Update")]    
    [HttpPatch("{eventId}/event-item/{eventItemId}")]
    public async Task<ActionResult<TicketClassDto>> PatchEventItem([FromRoute]Guid eventId, [FromRoute]Guid eventItemId,UpdateEventItemDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _eventService.UpdateEventItemAsync(userId, eventId,eventItemId, dto);
            return Ok(response);
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
    [Authorize(Policy = "Event.Update")]    
    [HttpDelete("{eventId}/event-item/{eventItemId}")]
    public async Task<ActionResult<bool>> DeleteEventItem([FromRoute]Guid eventId, [FromRoute]Guid eventItemId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _eventService.DeleteEventItemAsync(userId, eventId, eventItemId);
            return Ok(response);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new {message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Policy = "Event.Create")]
    [HttpPost("{eventId}/charts")]
    public async Task<ActionResult<ChartDto>> PostChart([FromRoute] Guid eventId,
        [FromBody] CreateChartDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var userId = GetCurrentUserId();
            var response = await _eventService.CreateChartAsync(userId, eventId, dto);
            return Ok(response);
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
    [Authorize(Policy = "Event.Update")]
    [HttpPut("{eventId}/charts/{chartId}")]
    public async Task<ActionResult<ChartDto>> UpdateChart([FromRoute] Guid eventId, 
        [FromRoute] Guid chartId,
        [FromBody] UpdateChartDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var userId = GetCurrentUserId();
            var response = await _eventService.UpdateChartAsync(userId, eventId,chartId, dto);
            return Ok(response);
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
    [HttpGet("{eventId}/charts")]
    public async Task<ActionResult<IEnumerable<ChartDto>>> GetEventCharts([FromRoute] Guid eventId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _eventService.GetEventChartsAsync(userId,eventId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpGet("{eventId}/charts/{chartId}")]
    public async Task<ActionResult<ChartDto>> GetEventCharts([FromRoute] Guid eventId, [FromRoute] Guid chartId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _eventService.GetEventChartAsync(userId,eventId,chartId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [Authorize(Policy = "Event.Update")]
    [HttpDelete("{eventId}/charts/{chartId}")]
    public async Task<ActionResult<bool>> DeleteChart([FromRoute] Guid eventId, [FromRoute] Guid chartId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _eventService.DeleteChartAsync(userId, eventId, chartId);
            return Ok(response);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Policy = "Event.Update")]
    [HttpPost("{eventId}/charts/{chartId}/sync-seats")]
    public async Task<ActionResult<SyncSeatsResponseDto>> SyncSeats(
        [FromRoute] Guid eventId, 
        [FromRoute] Guid chartId,
        [FromBody] SyncSeatsRequestDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _eventService.SyncSeatsAsync(userId, eventId, chartId, request.Seats, request.VenueDefinition, request.ChartKey);
            return Ok(response);
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

    [HttpGet("{eventId}/charts/{chartId}/seats")]
    public async Task<ActionResult<SeatMapViewDto>> GetSeatMapForView(
        [FromRoute] Guid eventId, 
        [FromRoute] Guid chartId,
        [FromQuery] Guid? eventItemId)
    {
        try
        {
            var userId = GetCurrentUserId();
            // Detect Org role explicitly if any role claim contains "Org"
            var roleClaims = User.FindAll("role")
                                 .Select(c => c.Value)
                                 .Concat(User.FindAll("roles").Select(c => c.Value))
                                 .Concat(User.FindAll("permission").Select(c => c.Value))
                                 .ToList();
            var userRole = roleClaims.Any(r => r == "Org")
                ? "Org"
                : roleClaims.FirstOrDefault() ?? "User";
            var response = await _eventService.GetSeatMapForViewAsync(userId, eventId, chartId, eventItemId, userRole);
            return Ok(response);
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

    [HttpPost("{eventId}/event-items/{eventItemId}/create-event-key")]
    [Authorize(Policy = "Event.Update")]
    public async Task<ActionResult<object>> CreateEventKeyForEventItem(
        [FromRoute] Guid eventId,
        [FromRoute] Guid eventItemId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var eventKey = await _eventService.CreateEventKeyForEventItemAsync(userId, eventId, eventItemId);
            return Ok(new { eventKey, message = "Event key created successfully" });
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

    /// <summary>
    /// Retrieve Seats.io event object by event key (with eventId and eventItemId)
    /// </summary>
    [HttpGet("{eventId}/event-items/{eventItemId}/seats-io-event")]
    [Authorize(Policy = "Event.Read")]
    public async Task<ActionResult<SeatsIoEventDto>> GetSeatsIoEventByEventItem(
        [FromRoute] Guid eventId,
        [FromRoute] Guid eventItemId,
        [FromQuery] string eventKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(eventKey))
            {
                return BadRequest(new { message = "Event key is required" });
            }

            // Retrieve event from Seats.io
            var seatService = HttpContext.RequestServices.GetRequiredService<ISeatService>();
            var seatsIoEvent = await seatService.RetrieveEventAsync(eventKey);
            
            return Ok(seatsIoEvent);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieve Seats.io event object by event key only (no eventId required)
    /// </summary>
    [HttpGet("seats-io-event")]
    public async Task<ActionResult<SeatsIoEventDto>> GetSeatsIoEvent([FromQuery] string eventKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(eventKey))
            {
                return BadRequest(new { message = "Event key is required" });
            }

            // Retrieve event from Seats.io
            var seatService = HttpContext.RequestServices.GetRequiredService<ISeatService>();
            var seatsIoEvent = await seatService.RetrieveEventAsync(eventKey);
            
            return Ok(seatsIoEvent);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Policy = "Event.Update")]
    [HttpPut("{eventId}/validate")]
    public async Task<ActionResult<EventValidationDto>> ValidateEvent([FromRoute] Guid eventId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _eventService.ValidateEventAsync(userId, eventId);
            return Ok(response);
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

    [Authorize(Policy = "Event.Update")]
    [HttpPost("{eventId}/submit")]
    public async Task<ActionResult<EventSubmissionResponseDto>> SubmitEvent([FromRoute] Guid eventId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _eventService.SubmitEventAsync(userId, eventId);
            return Ok(response);
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

    [Authorize(Policy = "Event.Update")]
    [HttpPost("{eventId}/cancel")]
    public async Task<ActionResult<bool>> CancelEvent([FromRoute] Guid eventId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _eventService.CancelEventAsync(userId, eventId);
            return Ok(response);
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

    [HttpGet("{eventId}/approval-history")]
    public async Task<ActionResult<IEnumerable<EventApprovalHistoryDto>>> GetApprovalHistory([FromRoute] Guid eventId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _eventService.GetApprovalHistoryAsync(userId, eventId);
            return Ok(response);
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

    [Authorize(Policy = "Event.Update")]
    [HttpGet("chart/{chartKey}/published-version")]
    public async Task<ActionResult> GetPublishedChartVersion([FromRoute] string chartKey)
    {
        try
        {
            var seatService = HttpContext.RequestServices.GetRequiredService<ISeatService>();
            var result = await seatService.RetrievePublishedChartVersionAsync(chartKey);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Policy = "Event.Update")]
    [HttpPost("{eventId}/event-items/{eventItemId}/reprocess-seats")]
    public async Task<ActionResult> ReProcessEventItemSeats([FromRoute] Guid eventId, [FromRoute] Guid eventItemId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _eventService.ReProcessEventItemSeatsAsync(userId, eventId, eventItemId);
            if (success)
            {
                return Ok(new { message = "Event item seats re-processed successfully" });
            }
            return BadRequest(new { message = "Failed to re-process event item seats" });
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
    /// Get event report detail from Seats.io
    /// GET /api/event/event-report-detail?eventKey={eventKey}
    /// Returns detailed information about all objects (seats) in the event including status, category, etc.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("event-report-detail")]
    public async Task<ActionResult> GetEventReportDetail([FromQuery] string eventKey)
    {
        try
        {
            if (string.IsNullOrEmpty(eventKey))
            {
                return BadRequest(new { message = "eventKey is required" });
            }

            var report = await _seatService.GetEventReportDetailAsync(eventKey);
            return Ok(report);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message, eventKey, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Get detailed chart report from Seats.io
    /// GET /api/event/chart-report-detail?chartKey={chartKey}
    /// Returns detailed information about all objects (seats) in the chart including category, type, etc.
    /// Note: Chart reports show the structure of the chart, not the actual booking status (which is in events)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("chart-report-detail")]
    public async Task<ActionResult> GetChartReportDetail([FromQuery] string chartKey)
    {
        try
        {
            if (string.IsNullOrEmpty(chartKey))
            {
                return BadRequest(new { message = "chartKey is required" });
            }

            var report = await _seatService.GetChartReportDetailAsync(chartKey);
            return Ok(report);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message, chartKey, stackTrace = ex.StackTrace });
        }
    }
}
