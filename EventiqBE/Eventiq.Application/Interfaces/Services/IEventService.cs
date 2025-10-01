using Eventiq.Application.Dtos;

namespace Eventiq.Application.Interfaces.Services;

public interface IEventService
{
    Task<CreateEventResponse> CreateEventInfoAsync(Guid userId, CreateEventDto dto);
    Task<PaymentInformationResponse> UpdateEventPaymentAsync(Guid userId, Guid eventId, UpdatePaymentInformation dto); //put
    Task<UpdateAddressResponse> UpdateEventAddressAsync(Guid userId, Guid eventId, UpdateEventAddressDto dto);  
    Task<EventDto> UpdateEventInfoAsync(Guid userId, Guid eventId, UpdateEventDto dto); 
    Task<EventDetail> GetByIdAsync(Guid id);
    Task ValidateEventOwnerAsync(Guid userId, List<Guid> orgIds);
    Task<TicketClassDto> CreateTicketClassAsync(Guid userId, Guid eventId, CreateTicketClassDto dto);
    Task<TicketClassDto> UpdateTicketClassInfoAsync(Guid userId, Guid eventId, Guid ticketClassId, UpdateTicketClassInfoDto dto);
    Task<IEnumerable<TicketClassDto>> GetEventTicketClassesAsync(Guid userId, Guid eventId);
    Task<IEnumerable<EventItemDto>> GetEventItemAsync(Guid eventId);
    Task<EventItemDto> CreateEventItemAsync(Guid userId, Guid eventId, CreateEventItemDto dto);
    Task<EventItemDto> UpdateEventItemAsync(Guid userId, Guid eventId, Guid eventItemId, UpdateEventItemDto dto);
    Task<bool> DeleteEventItemAsync(Guid userId, Guid eventId, Guid eventItemId);
    Task<PaginatedResult<EventPreview>> GetEventsByOrganizationAsync(Guid userId, Guid orgId, int page = 1, int pageSize = 10);
    Task<ChartDto> CreateChartAsync(Guid userId, Guid eventId, CreateChartDto dto);
    Task<ChartDto> UpdateChartAsync(Guid userId, Guid eventId, Guid chartId, UpdateChartDto dto);
    Task<IEnumerable<ChartDto>> GetEventChartAsync(Guid userId, Guid eventId);
    Task<bool> DeleteChartAsync(Guid userId, Guid eventId, Guid chartId);
}

