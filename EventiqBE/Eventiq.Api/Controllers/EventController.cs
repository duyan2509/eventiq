using System.Data;
using AutoMapper;
using Eventiq.Api.Request;
using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eventiq.Api.Controllers;

[Authorize]
public class EventController:BaseController
{
    protected readonly IMapper _mapper;

    public EventController(IMapper mapper, IEventService eventService)
    {
        _mapper = mapper;
        _eventService = eventService;
    }

    protected readonly IEventService _eventService;
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
            if (!ModelState.IsValid && !dto.CheckValidTime())
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
    [HttpPut("{eventId}/event-item/{eventItemId}/chart-key")]
    public async Task<ActionResult<TicketClassDto>> PutEventItemChartKey([FromRoute]Guid eventId, [FromRoute]Guid eventItemId,EventCharKey dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _eventService.UpdateChartKeyAsync(userId, eventId,eventItemId, dto);
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
    [HttpDelete("{eventId}/event-item/{eventItemId}")]
    public async Task<ActionResult<bool>> DeleteOrganization([FromRoute]Guid eventId, [FromRoute]Guid eventItemId)
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
}