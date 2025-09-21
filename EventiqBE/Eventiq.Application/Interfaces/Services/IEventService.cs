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
}