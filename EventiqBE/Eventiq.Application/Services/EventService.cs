using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces.Services;

namespace Eventiq.Application.Services;

public class EventService:IEventService
{
    public Task<EventDto> CreateEventInfoAsync(Guid userId, CreateEventDto dto)
    {
        throw new NotImplementedException();
    }

    public Task<EventDto> UpdateEventPaymentAsync(Guid userId, Guid eventId, UpdatePaymentInformation dto)
    {
        throw new NotImplementedException();
    }

    public Task<EventDto> UpdateEventAddressAsync(Guid userId, Guid eventId, UpdateEventAddressDto dto)
    {
        throw new NotImplementedException();
    }

    public Task<EventDto> UpdateEventInfoAsync(Guid userId, Guid eventId, UpdateEventDto dto)
    {
        throw new NotImplementedException();
    }

    public Task<EventDto> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }
}