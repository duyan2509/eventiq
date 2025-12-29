using Eventiq.Application.Dtos;
using Eventiq.Domain.Entities;

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
    Task<IEnumerable<ChartDto>> GetEventChartsAsync(Guid userId, Guid eventId);
    Task<ChartDto> GetEventChartAsync(Guid userId, Guid eventId, Guid chartId);
    Task<bool> DeleteChartAsync(Guid userId, Guid eventId, Guid chartId);
    
    // New methods for seat map sync and event management
    Task<SyncSeatsResponseDto> SyncSeatsAsync(Guid userId, Guid eventId, Guid chartId, IEnumerable<SeatInfoDto> seatsData, string? venueDefinition = null, string? chartKey = null);
    Task<SeatMapViewDto> GetSeatMapForViewAsync(Guid userId, Guid eventId, Guid chartId, Guid? eventItemId, string userRole);
    Task<EventValidationDto> ValidateEventAsync(Guid userId, Guid eventId);
    Task<EventSubmissionResponseDto> SubmitEventAsync(Guid userId, Guid eventId);
    Task<bool> CancelEventAsync(Guid userId, Guid eventId);
    Task<IEnumerable<EventApprovalHistoryDto>> GetApprovalHistoryAsync(Guid userId, Guid eventId);
    Task<IEnumerable<EventApprovalHistoryDto>> GetApprovalHistoryForAdminAsync(Guid eventId);
    
    // Admin methods
    Task<PaginatedResult<EventPreview>> GetAllEventsAsync(int page = 1, int size = 10, EventStatus? status = null);
    Task<EventSubmissionResponseDto> ApproveEventAsync(Guid adminUserId, Guid eventId, string? comment = null);
    Task<EventSubmissionResponseDto> RejectEventAsync(Guid adminUserId, Guid eventId, string comment);
    
    // Customer/public methods
    Task<CustomerEventListDto> GetPublishedEventsAsync();
    Task<CustomerEventDetailDto> GetPublishedEventDetailAsync(Guid eventId);
    Task<CustomerSeatMapDto> GetEventItemSeatMapAsync(Guid eventId, Guid eventItemId);
    
    // Create Seats.io event key for eventItem
    Task<string> CreateEventKeyForEventItemAsync(Guid userId, Guid eventId, Guid eventItemId);
    
    // Re-process event item to fetch and save seats from Seats.io
    Task<bool> ReProcessEventItemSeatsAsync(Guid userId, Guid eventId, Guid eventItemId);
}

