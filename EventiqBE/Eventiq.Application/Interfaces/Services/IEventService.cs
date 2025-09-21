using Eventiq.Application.Dtos;

namespace Eventiq.Application.Interfaces.Services;

public interface IEventService
{
    Task<CreateEventResponse> CreateEventInfoAsync(Guid userId, CreateEventDto dto);
    Task<PaymentInformationResponse> UpdateEventPaymentAsync(Guid userId, Guid eventId, UpdatePaymentInformation dto); //put
    Task<UpdateAddressResponse> UpdateEventAddressAsync(Guid userId, Guid eventId, UpdateEventAddressDto dto);  
    Task<EventDto> UpdateEventInfoAsync(Guid userId, Guid eventId, UpdateEventDto dto); 
    Task<EventDto> GetByIdAsync(Guid id);
    Task ValidateEventOwnerAsync(Guid userId, List<Guid> orgIds);
    Task<TicketClassDto> CreateTicketClassAsync(Guid userId, Guid eventId, CreateTicketClassDto dto);
    Task<TicketClassDto> UpdateTicketClassInfoAsync(Guid userId, Guid eventId, Guid ticketClassId, UpdateTicketClassInfoDto dto);
    Task<IEnumerable<TicketClassDto>> GetEventTicketClassesAsync(Guid userId, Guid eventId);
}