using System.Data;
using AutoMapper;
using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces;
using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Application.Interfaces.Services;
using Eventiq.Domain.Entities;

namespace Eventiq.Application.Services;

public class EventService:IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;
    private readonly ICloudStorageService _storageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventAddressRepository _eventAddressRepository;
    private readonly IOrganizationRepository  _organizationRepository;
    private readonly IChartRepository _chartRepository;

    private readonly IEventItemRepository _eventItemRepository;

    public EventService(IEventRepository eventRepository, IMapper mapper, ICloudStorageService storageService, IUnitOfWork unitOfWork, IEventAddressRepository eventAddressRepository, IOrganizationRepository organizationRepository, IChartRepository chartRepository, IEventItemRepository eventItemRepository, IIdentityService identityService, ITicketClassRepository ticketClassRepository, ISeatService seatService, IEventSeatRepository eventSeatRepository, IEventSeatStateRepository eventSeatStateRepository, IEventApprovalHistoryRepository eventApprovalHistoryRepository, IMessageQueueService messageQueueService)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
        _storageService = storageService;
        _unitOfWork = unitOfWork;
        _eventAddressRepository = eventAddressRepository;
        _organizationRepository = organizationRepository;
        _chartRepository = chartRepository;
        _eventItemRepository = eventItemRepository;
        _identityService = identityService;
        _ticketClassRepository = ticketClassRepository;
        _seatService = seatService;
        _eventSeatRepository = eventSeatRepository;
        _eventSeatStateRepository = eventSeatStateRepository;
        _eventApprovalHistoryRepository = eventApprovalHistoryRepository;
        _messageQueueService = messageQueueService;
    }

    private readonly IIdentityService _identityService;
    private readonly ITicketClassRepository _ticketClassRepository;
    private readonly ISeatService _seatService;
    private readonly IEventSeatRepository _eventSeatRepository;
    private readonly IEventSeatStateRepository _eventSeatStateRepository;
    private readonly IEventApprovalHistoryRepository _eventApprovalHistoryRepository;
    private readonly IMessageQueueService _messageQueueService;



    public async Task<CreateEventResponse> CreateEventInfoAsync(Guid userId, CreateEventDto dto)
    {
        string? uploadedUrl = null;
        var org = await _organizationRepository.GetByIdAsync(dto.OrganizationId);
        if (org == null)
            throw new Exception("Organization not found");
        try
        {
            await _unitOfWork.BeginTransactionAsync();
            var evnt = _mapper.Map<Event>(dto);
            await _eventRepository.AddAsync(evnt);
            uploadedUrl = await _storageService.UploadAsync(dto.BannerStream, evnt.Id.ToString());
            if (uploadedUrl != null)
            {
                evnt.Banner = uploadedUrl;
                await _eventRepository.UpdateAsync(evnt);
            }
            else
                throw new Exception("Banner upload failed");
            var address = _mapper.Map<EventAddress>(dto.EventAddress);
            address.EventId = evnt.Id;
            await _eventAddressRepository.AddAsync(address);
            await _unitOfWork.CommitAsync();
            evnt.EventAddress = address;

            return _mapper.Map<CreateEventResponse>(evnt);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            if (uploadedUrl != null) 
                await _storageService.DeleteAsync(uploadedUrl);
            throw;
        }
    }
    public async Task<EventDto> UpdateEventInfoAsync(Guid userId, Guid eventId, UpdateEventDto dto)
    {

        string? uploadedUrl = null;
        if (dto.OrganizationId != null)
        {
            var orgId = dto.OrganizationId.Value;
            var org = await _organizationRepository.GetByIdAsync(orgId);
            if (org == null)
                throw new Exception("Organization not found");            
        }
        try
        {
            await _unitOfWork.BeginTransactionAsync();
            var evnt = await _eventRepository.GetByIdAsync(eventId);
            if (evnt == null)
                throw new Exception("Event not found");
            if (dto.OrganizationId != null)
            {
                await ValidateEventOwnerAsync(userId, [evnt.OrganizationId,dto.OrganizationId.Value]);
                evnt.OrganizationId = dto.OrganizationId.Value;
            }
            if(dto.Name != null)
                evnt.Name = dto.Name;
            if (dto.Description != null)
                evnt.Description = dto.Description;
            await _eventRepository.UpdateAsync(evnt);
            if (dto.BannerStream != null)
            {
                await _storageService.DeleteAsync(evnt.Banner);
                uploadedUrl = await _storageService.UploadAsync(dto.BannerStream, evnt.Id.ToString());
                if (uploadedUrl != null)
                {
                    evnt.Banner = uploadedUrl;
                    await _eventRepository.UpdateAsync(evnt);
                }
                else
                    throw new Exception("Banner upload failed");
            }
            await _unitOfWork.CommitAsync();

            return _mapper.Map<EventDto>(evnt);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            if (uploadedUrl != null) 
                await _storageService.DeleteAsync(uploadedUrl);
            throw;
        }
    }
    public async Task<PaymentInformationResponse> UpdateEventPaymentAsync(Guid userId, Guid eventId, UpdatePaymentInformation dto)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new Exception("Event not found");
        await ValidateEventOwnerAsync(userId,[evnt.OrganizationId]);
        evnt.BankCode = dto.BankCode;
        evnt.AccountNumber = dto.AccountNumber;
        evnt.AccountName = dto.AccountName;
        await _eventRepository.UpdateAsync(evnt);
        return _mapper.Map<PaymentInformationResponse>(evnt);
    }

    public async Task<UpdateAddressResponse> UpdateEventAddressAsync(Guid userId, Guid eventId, UpdateEventAddressDto dto)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new Exception("Event not found");
        await ValidateEventOwnerAsync(userId, [evnt.OrganizationId]);
        var address = await _eventAddressRepository.GetByEventIdAsync(eventId);
        if(address == null)
            throw new Exception("Address not found");
        if(dto.ProvinceCode!=null)
            address.ProvinceCode = dto.ProvinceCode;
        if(dto.ProvinceName!=null)
            address.ProvinceName = dto.ProvinceName;
        if(dto.ProvinceName!=null)
            address.ProvinceName = dto.ProvinceName;
        if(dto.CommuneName!=null)
            address.CommuneName = dto.CommuneName;
        if(dto.Detail!=null)
            address.Detail = dto.Detail;
        await _eventAddressRepository.UpdateAsync(address);
        return _mapper.Map<UpdateAddressResponse>(address);
    }
    public async Task<EventDetail> GetByIdAsync(Guid id)
    {
        var evnt = await   _eventRepository.GetDetailEventAsync(id);
        if (evnt == null)
            throw new KeyNotFoundException("Event not found");
        return _mapper.Map<EventDetail>(evnt);
        
    }

    public async Task ValidateEventOwnerAsync(Guid userId, List<Guid> orgIds)
    {
        var userOrgs = await _identityService.GetUserOrgsAsync(userId);
        orgIds.ForEach(orgId =>
        {
            if (!userOrgs.Contains(orgId))
                throw new UnauthorizedAccessException($"User does not belong to organization {orgId}");
        });
    }

    public async Task<TicketClassDto> CreateTicketClassAsync(Guid userId, Guid eventId, CreateTicketClassDto dto)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new Exception("Event not found");
        await ValidateEventOwnerAsync(userId, [evnt.OrganizationId]);
        var ticketClass = _mapper.Map<TicketClass>(dto);
        ticketClass.EventId= evnt.Id;
        await _ticketClassRepository.AddAsync(ticketClass);
        return  _mapper.Map<TicketClassDto>(ticketClass);
    }
    public async Task<TicketClassDto> UpdateTicketClassInfoAsync(Guid userId, Guid eventId, Guid ticketClassId, UpdateTicketClassInfoDto dto)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new Exception("Event not found");
        await ValidateEventOwnerAsync(userId, [evnt.OrganizationId]);
        var ticketClass = await _ticketClassRepository.GetByIdAsync(ticketClassId);
        if (ticketClass == null)
            throw new Exception("Ticket class not found");
        
        if (dto.SaleEnd.HasValue && dto.SaleStart.HasValue)
        {
            if (dto.SaleEnd > dto.SaleStart)
            {
                ticketClass.SaleEnd = dto.SaleEnd.Value;
                ticketClass.SaleStart = dto.SaleStart.Value;
            }
            else throw new Exception("Invalid sale time");
        }
        else if (dto.SaleEnd.HasValue)
        {
            if (dto.SaleEnd > ticketClass.SaleStart)
            {
                ticketClass.SaleEnd = dto.SaleEnd.Value;
            }
            else throw new Exception($"Sale end must be after sale start {ticketClass.SaleStart}");
        }
        else if (dto.SaleStart.HasValue)
        {
            if (ticketClass.SaleEnd > dto.SaleStart)
            {
                ticketClass.SaleStart = dto.SaleStart.Value;
            }
            else throw new Exception($"Sale start must be before sale end {ticketClass.SaleEnd}");
        } 
        
        if(!string.IsNullOrEmpty(dto.Name))
            ticketClass.Name = dto.Name;
        if(dto.Price!=null)
            ticketClass.Price = dto.Price.Value;
        if(dto.MaxPerUser!=null)
            ticketClass.MaxPerUser = dto.MaxPerUser.Value;
        await _ticketClassRepository.UpdateAsync(ticketClass);
        return _mapper.Map<TicketClassDto>(ticketClass);
    }

    public async Task<IEnumerable<TicketClassDto>> GetEventTicketClassesAsync(Guid userId, Guid eventId)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new Exception("Event not found");
        await ValidateEventOwnerAsync(userId, [evnt.OrganizationId]);
        var ticketClasses = await _ticketClassRepository.GetEventTicketClassesAsync(eventId);
        var result = ticketClasses.Select(ticketClass => _mapper.Map<TicketClassDto>(ticketClass));
        return result;
    }

    public async Task<IEnumerable<EventItemDto>> GetEventItemAsync(Guid eventId)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new Exception("Event not found");
        var evntItems = await _eventItemRepository.GetAllByEventIdAsync(eventId);
        var dtos = evntItems.Select(eventItem => _mapper.Map<EventItemDto>(eventItem));
        return dtos;
        
    }

    public async Task<EventItemDto> CreateEventItemAsync(Guid userId, Guid eventId, CreateEventItemDto dto)
    {
        if(dto.Start <  DateTime.UtcNow.AddDays(1))
            throw new Exception($"Start date must be after submit date at least 1 day");
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new Exception("Event not found");
        await ValidateEventOwnerAsync(userId, [evnt.OrganizationId]);
        var evntItems = await _eventItemRepository.GetAllByEventIdAsync(eventId);
        foreach (var evntItem in evntItems)
        {
            if ((evntItem.Start <= dto.Start && dto.End <= evntItem.End)
            ||(dto.Start <= evntItem.Start && dto.End <= evntItem.End)
            || (evntItem.Start <= dto.Start && evntItem.End <= dto.End))
                throw new Exception($"Event item is overlapped with {evntItem.Name} at [{evntItem.Start} and {evntItem.End}]");
        }
        var eventItem = _mapper.Map<EventItem>(dto); 
        var chart = await _chartRepository.GetByIdAsync(dto.ChartId);
        if(chart == null)
            throw new Exception("Chart not found");
        if(chart.EventId!=eventId)
            throw new Exception("Chart and event item are not in the same event");
        eventItem.Chart =  chart;
        eventItem.EventId = eventId;
        await _eventItemRepository.AddAsync(eventItem);
        return _mapper.Map<EventItemDto>(eventItem);
    }

    public async Task<EventItemDto> UpdateEventItemAsync(Guid userId, Guid eventId, Guid eventItemId, UpdateEventItemDto dto)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new Exception("Event not found");
        await ValidateEventOwnerAsync(userId, [evnt.OrganizationId]);
        var eventItem = await _eventItemRepository.GetByDetailByIdAsync(eventItemId);
        if(eventItem == null)
            throw new Exception("Event item not found");

        try
        {
            await _unitOfWork.BeginTransactionAsync();
            if (dto.Start != null || dto.End != null)
            {
                var evntItems = await _eventItemRepository.GetAllByEventIdAsync(eventId);
                if(dto.Start == null)
                    dto.Start = eventItem.Start;
                if(dto.End == null)
                    dto.End = eventItem.End;
                foreach (var evntItem in evntItems)
                {
                    if (evntItem.Id == eventItem.Id)
                    {
                        evntItem.Start = eventItem.Start;
                    }
                    else if ((evntItem.Start <= dto.Start && dto.End <= evntItem.End)
                        ||(dto.Start <= evntItem.Start && dto.End <= evntItem.End)
                        || (evntItem.Start <= dto.Start && evntItem.End <= dto.End))
                        throw new Exception($"Event item is overlapped with {evntItem.Name} at [{evntItem.Start} and {evntItem.End}]");
                }
                eventItem.Start = dto.Start.Value;
                eventItem.Start = dto.Start.Value;
            }

            if (dto.Name != null)
                eventItem.Name = dto.Name;
            if (dto.Description != null)
                eventItem.Description = dto.Description;
            if (dto.ChartId != null)
            {
                var chart = await _chartRepository.GetByIdAsync(dto.ChartId.Value);
                if(chart == null)
                    throw new Exception("Chart not found");
                if(chart.EventId!=eventItem.EventId)
                    throw new Exception("Chart and event item are not in the same event");
                eventItem.Chart =  chart;
            }
            await _eventItemRepository.UpdateAsync(eventItem);
            await _unitOfWork.CommitAsync();
            return _mapper.Map<EventItemDto>(eventItem);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
        
    }


    public async Task<bool> DeleteEventItemAsync(Guid userId, Guid eventId, Guid eventItemId)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new Exception("Event not found");
        await ValidateEventOwnerAsync(userId, [evnt.OrganizationId]);
        if(evnt.Status==EventStatus.Published || evnt.Status==EventStatus.Pending)
            throw new Exception("Only delete event item of draft event");
        var eventItem = _eventItemRepository.GetByIdAsync(eventItemId);
        if(eventItem == null)
            throw new Exception("Event item not found");
        await _eventItemRepository.HardDeleteAsync(eventId);
        return true;
    }

    public async Task<PaginatedResult<EventPreview>> GetEventsByOrganizationAsync(Guid userId, Guid orgId, int page = 1, int size = 10)
    {
        await CheckUserOrganizationAsync(userId,orgId);
        var evnts = await _eventRepository.GetByOrgAsync(orgId,page,size);
        var dtos = evnts.Data.Select(evnt => _mapper.Map<EventPreview>(evnt));
        return new PaginatedResult<EventPreview>
        {
            Data = dtos,
            Page = evnts.Page,
            Size = evnts.Size,
            Total = evnts.Total
        };
    }

    public async Task<ChartDto> CreateChartAsync(Guid userId, Guid eventId, CreateChartDto dto)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new Exception("Event not found");
        await ValidateEventOwnerAsync(userId, [evnt.OrganizationId]);
        var ticketClasses = await _ticketClassRepository.GetEventTicketClassesAsync(eventId);
        if (ticketClasses == null || !ticketClasses.Any())
            throw new Exception("Ticket class not found");
        var chart = _mapper.Map<Chart>(dto);
        chart.EventId= evnt.Id;
        chart.Key = await _seatService.CreateChartAsync(ticketClasses);
        await _chartRepository.AddAsync(chart);
        return  _mapper.Map<ChartDto>(chart);
    }

    public async Task<ChartDto> UpdateChartAsync(Guid userId, Guid eventId, Guid chartId, UpdateChartDto dto)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new Exception("Event not found");
        await ValidateEventOwnerAsync(userId, [evnt.OrganizationId]);
        var chart = await _chartRepository.GetByIdAsync(chartId);
        if (chart == null)
            throw new Exception("Chart not found");
        chart.Name = dto.Name;
        await _chartRepository.UpdateAsync(chart);
        return  _mapper.Map<ChartDto>(chart);
    }

    public async Task<IEnumerable<ChartDto>> GetEventChartsAsync(Guid userId, Guid eventId)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new Exception("Event not found");
        await ValidateEventOwnerAsync(userId, [evnt.OrganizationId]);

        var charts = await _chartRepository.GetByEventItAsync(eventId);
        return charts.Select(chart => _mapper.Map<ChartDto>(chart));
    }

    public async Task<ChartDto> GetEventChartAsync(Guid userId, Guid eventId, Guid chartId)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new Exception("Event not found");
        await ValidateEventOwnerAsync(userId, [evnt.OrganizationId]);
        var chart = await _chartRepository.GetByIdAsync(chartId);
        if (chart == null)
            throw new Exception("Chart not found");
        return _mapper.Map<ChartDto>(chart);
    }

    public async Task<bool> DeleteChartAsync(Guid userId, Guid eventId, Guid chartId)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new Exception("Event not found");
        await ValidateEventOwnerAsync(userId, [evnt.OrganizationId]);
        var chart = await _chartRepository.GetDetailChartByIdAsync(chartId);
        if(chart==null)
            throw new Exception("Chart not found");
        if (chart.EventItems.Any())
            throw new Exception("Cannot delete chart because it is used in at least one showtime");
        await _chartRepository.HardDeleteAsync(chartId);
        return true;
    }

    public async Task<bool> CheckUserOrganizationAsync(Guid userId, Guid orgId)
    {
        var org = await _organizationRepository.GetByUserIdAsync(userId,orgId);
        if(org==null)
            throw new UnauthorizedAccessException("Organization not found");
        return true;
    }

    public async Task<SyncSeatsResponseDto> SyncSeatsAsync(Guid userId, Guid eventId, Guid chartId, IEnumerable<SeatInfoDto> seatsData, string? venueDefinition = null)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new Exception("Event not found");
        await ValidateEventOwnerAsync(userId, [evnt.OrganizationId]);

        var chart = await _chartRepository.GetByIdAsync(chartId);
        if (chart == null || chart.EventId != eventId)
            throw new Exception("Chart not found");

        // Lưu venue definition vào chart nếu có
        if (!string.IsNullOrEmpty(venueDefinition))
        {
            chart.VenueDefinition = venueDefinition;
            await _chartRepository.UpdateAsync(chart);
        }

        // Convert seats data from frontend (from Seats.io designer) to EventSeat entities
        var eventSeats = seatsData.Select(s => new EventSeat
        {
            ChartId = chartId,
            SeatKey = s.SeatKey,
            Label = s.Label,
            Section = s.Section,
            Row = s.Row,
            Number = s.Number,
            CategoryKey = s.CategoryKey,
            ExtraData = s.ExtraData != null ? System.Text.Json.JsonSerializer.Serialize(s.ExtraData) : null
        }).ToList();

        // Bulk upsert seats
        var existingSeats = await _eventSeatRepository.GetByChartIdAsync(chartId);
        var existingSeatKeys = existingSeats.Select(s => s.SeatKey).ToHashSet();
        
        int newSeats = eventSeats.Count(s => !existingSeatKeys.Contains(s.SeatKey));
        
        await _eventSeatRepository.BulkUpsertAsync(eventSeats);
        
        // Create EventSeatState for all event items that use this chart
        var eventItems = await _eventItemRepository.GetAllByEventIdAsync(eventId);
        var chartEventItems = eventItems.Where(ei => ei.ChartId == chartId).ToList();
        
        var updatedSeats = await _eventSeatRepository.GetByChartIdAsync(chartId);
        
        foreach (var eventItem in chartEventItems)
        {
            foreach (var seat in updatedSeats)
            {
                var existingState = await _eventSeatStateRepository.GetByEventItemAndSeatAsync(eventItem.Id, seat.Id);
                if (existingState == null)
                {
                    var newState = new EventSeatState
                    {
                        EventItemId = eventItem.Id,
                        EventSeatId = seat.Id,
                        Status = SeatStatus.Free
                    };
                    await _eventSeatStateRepository.AddAsync(newState);
                }
            }
        }

        return new SyncSeatsResponseDto
        {
            TotalSeats = eventSeats.Count,
            NewSeats = newSeats,
            UpdatedSeats = eventSeats.Count - newSeats,
            Success = true
        };
    }

    public async Task<SeatMapViewDto> GetSeatMapForViewAsync(Guid userId, Guid eventId, Guid chartId, Guid? eventItemId, string userRole)
    {
        var chart = await _chartRepository.GetByIdAsync(chartId);
        if (chart == null || chart.EventId != eventId)
            throw new Exception("Chart not found");

        // Check permission: Org can view any, users can only view published events
        if (userRole != "Org")
        {
            var evnt = await _eventRepository.GetByIdAsync(eventId);
            if (evnt == null || evnt.Status != EventStatus.Published)
                throw new UnauthorizedAccessException("Event not available");
        }

        var seats = await _eventSeatRepository.GetByChartIdAsync(chartId);
        var seatDtos = new List<SeatWithStatusDto>();

        if (eventItemId.HasValue)
        {
            // Get seat states for this event item
            var seatStates = await _eventSeatStateRepository.GetByEventItemIdAsync(eventItemId.Value);
            var stateMap = seatStates.ToDictionary(s => s.EventSeatId, s => s);

            foreach (var seat in seats)
            {
                var state = stateMap.ContainsKey(seat.Id) ? stateMap[seat.Id] : null;
                seatDtos.Add(new SeatWithStatusDto
                {
                    EventSeatId = seat.Id,
                    SeatKey = seat.SeatKey,
                    Label = seat.Label,
                    Section = seat.Section,
                    Row = seat.Row,
                    Number = seat.Number,
                    CategoryKey = seat.CategoryKey,
                    Status = state?.Status == SeatStatus.Paid ? "paid" : "free",
                    ExtraData = seat.ExtraData != null ? System.Text.Json.JsonSerializer.Deserialize<object>(seat.ExtraData) : null
                });
            }
        }
        else
        {
            // No event item specified - show all as free (for org config view)
            foreach (var seat in seats)
            {
                seatDtos.Add(new SeatWithStatusDto
                {
                    EventSeatId = seat.Id,
                    SeatKey = seat.SeatKey,
                    Label = seat.Label,
                    Section = seat.Section,
                    Row = seat.Row,
                    Number = seat.Number,
                    CategoryKey = seat.CategoryKey,
                    Status = "free",
                    ExtraData = seat.ExtraData != null ? System.Text.Json.JsonSerializer.Deserialize<object>(seat.ExtraData) : null
                });
            }
        }

        return new SeatMapViewDto
        {
            ChartId = chartId,
            ChartKey = chart.Key,
            ChartName = chart.Name,
            VenueDefinition = chart.VenueDefinition,
            Seats = seatDtos,
            IsReadOnly = userRole != "Org" // Only Org can edit
        };
    }

    public async Task<EventValidationDto> ValidateEventAsync(Guid userId, Guid eventId)
    {
        var evnt = await _eventRepository.GetDetailEventAsync(eventId);
        if (evnt == null)
            throw new Exception("Event not found");
        await ValidateEventOwnerAsync(userId, [evnt.OrganizationId]);

        var validation = new EventValidationDto { IsValid = true };

        // Validate event info
        if (string.IsNullOrWhiteSpace(evnt.Name))
            validation.Errors.Add("Event name is required");

        if (evnt.EventAddress == null)
            validation.Errors.Add("Event address is required");
        else
        {
            if (string.IsNullOrWhiteSpace(evnt.EventAddress.Detail))
                validation.Errors.Add("Event address detail is required");
        }

        if (string.IsNullOrWhiteSpace(evnt.AccountNumber) || string.IsNullOrWhiteSpace(evnt.AccountName))
            validation.Errors.Add("Payment information is required");

        // Validate ticket classes
        var ticketClasses = await _ticketClassRepository.GetEventTicketClassesAsync(eventId);
        if (!ticketClasses.Any())
            validation.Errors.Add("At least one ticket class is required");

        // Validate charts and seats
        var charts = await _chartRepository.GetByEventItAsync(eventId);
        if (!charts.Any())
            validation.Warnings.Add("No seat maps configured");
        else
        {
            foreach (var chart in charts)
            {
                var seats = await _eventSeatRepository.GetByChartIdAsync(chart.Id);
                if (!seats.Any())
                    validation.Warnings.Add($"Chart '{chart.Name}' has no seats synchronized");
            }
        }

        // Validate event items
        var eventItems = await _eventItemRepository.GetAllByEventIdAsync(eventId);
        if (!eventItems.Any())
            validation.Errors.Add("At least one event item (showtime) is required");

        validation.IsValid = !validation.Errors.Any();
        return validation;
    }

    public async Task<EventSubmissionResponseDto> SubmitEventAsync(Guid userId, Guid eventId)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new Exception("Event not found");
        await ValidateEventOwnerAsync(userId, [evnt.OrganizationId]);

        if (evnt.Status != EventStatus.Draft)
            throw new Exception($"Cannot submit event with status {evnt.Status}");

        // Validate before submitting
        var validation = await ValidateEventAsync(userId, eventId);
        if (!validation.IsValid)
            throw new Exception($"Event validation failed: {string.Join(", ", validation.Errors)}");

        var previousStatus = evnt.Status;
        evnt.Status = EventStatus.Pending;
        await _eventRepository.UpdateAsync(evnt);

        // Create approval history (submitted by org, not approved yet)
        var history = new EventApprovalHistory
        {
            EventId = eventId,
            PreviousStatus = previousStatus,
            NewStatus = EventStatus.Pending,
            Comment = null,
            ApprovedByUserId = null, // Chưa được approve, chỉ submit
            ApprovedByUserName = null,
            ActionDate = DateTime.UtcNow
        };
        await _eventApprovalHistoryRepository.AddAsync(history);

        return new EventSubmissionResponseDto
        {
            Success = true,
            Message = "Event submitted successfully for approval",
            NewStatus = EventStatus.Pending
        };
    }

    public async Task<bool> CancelEventAsync(Guid userId, Guid eventId)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new Exception("Event not found");
        await ValidateEventOwnerAsync(userId, [evnt.OrganizationId]);

        if (evnt.Status == EventStatus.Published)
            throw new Exception("Cannot cancel published event");

        var previousStatus = evnt.Status;
        evnt.Status = EventStatus.Draft;
        await _eventRepository.UpdateAsync(evnt);

        // Create approval history (cancelled by org, not an approval action)
        var history = new EventApprovalHistory
        {
            EventId = eventId,
            PreviousStatus = previousStatus,
            NewStatus = EventStatus.Draft,
            Comment = "Event cancelled by organizer",
            ApprovedByUserId = null, // Không phải approval action
            ApprovedByUserName = null,
            ActionDate = DateTime.UtcNow
        };
        await _eventApprovalHistoryRepository.AddAsync(history);

        return true;
    }

    public async Task<IEnumerable<EventApprovalHistoryDto>> GetApprovalHistoryAsync(Guid userId, Guid eventId)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new Exception("Event not found");
        
        // Check permission: Org owner can view (Admin check will be in controller)
        await ValidateEventOwnerAsync(userId, [evnt.OrganizationId]);

        var histories = await _eventApprovalHistoryRepository.GetByEventIdAsync(eventId);
        return histories.Select(h => new EventApprovalHistoryDto
        {
            Id = h.Id,
            EventId = h.EventId,
            PreviousStatus = h.PreviousStatus.ToString(),
            NewStatus = h.NewStatus.ToString(),
            Comment = h.Comment,
            ApprovedByUserId = h.ApprovedByUserId,
            ApprovedByUserName = h.ApprovedByUserName,
            ActionDate = h.ActionDate
        });
    }

    public async Task<IEnumerable<EventApprovalHistoryDto>> GetApprovalHistoryForAdminAsync(Guid eventId)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new Exception("Event not found");

        var histories = await _eventApprovalHistoryRepository.GetByEventIdAsync(eventId);
        return histories.Select(h => new EventApprovalHistoryDto
        {
            Id = h.Id,
            EventId = h.EventId,
            PreviousStatus = h.PreviousStatus.ToString(),
            NewStatus = h.NewStatus.ToString(),
            Comment = h.Comment,
            ApprovedByUserId = h.ApprovedByUserId,
            ApprovedByUserName = h.ApprovedByUserName,
            ActionDate = h.ActionDate
        });
    }

    public async Task<PaginatedResult<EventPreview>> GetAllEventsAsync(int page = 1, int size = 10, EventStatus? status = null)
    {
        var result = await _eventRepository.GetAllAsync(page, size, status);
        var dtos = result.Data.Select(e => _mapper.Map<EventPreview>(e));
        return new PaginatedResult<EventPreview>
        {
            Data = dtos,
            Page = result.Page,
            Size = result.Size,
            Total = result.Total
        };
    }

    public async Task<EventSubmissionResponseDto> ApproveEventAsync(Guid adminUserId, Guid eventId, string? comment = null)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new Exception("Event not found");

        if (evnt.Status != EventStatus.Pending)
            throw new Exception($"Cannot approve event with status {evnt.Status}");

        var previousStatus = evnt.Status;
        
        // Cập nhật trạng thái event sang InProgress (đang xử lý tạo seat map và vé)
        evnt.Status = EventStatus.InProgress;
        await _eventRepository.UpdateAsync(evnt);

        // Get admin user info
        var adminUser = await _identityService.GetByIdAsync(adminUserId);

        // Create approval history
        var history = new EventApprovalHistory
        {
            EventId = eventId,
            PreviousStatus = previousStatus,
            NewStatus = EventStatus.InProgress,
            Comment = comment,
            ApprovedByUserId = adminUserId,
            ApprovedByUserName = adminUser?.Email ?? "Admin",
            ActionDate = DateTime.UtcNow
        };
        await _eventApprovalHistoryRepository.AddAsync(history);

        // Gửi message vào queue để xử lý tạo seat map và vé
        var message = new EventProcessingMessage
        {
            EventId = eventId,
            AdminUserId = adminUserId,
            RequestedAt = DateTime.UtcNow
        };
        await _messageQueueService.PublishEventProcessingMessageAsync(message);

        return new EventSubmissionResponseDto
        {
            Success = true,
            Message = "Event approval initiated. Seat map and tickets will be created by background worker.",
            NewStatus = EventStatus.InProgress
        };
    }

    public async Task<EventSubmissionResponseDto> RejectEventAsync(Guid adminUserId, Guid eventId, string comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
            throw new Exception("Rejection comment is required");

        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new Exception("Event not found");

        if (evnt.Status != EventStatus.Pending)
            throw new Exception($"Cannot reject event with status {evnt.Status}");

        var previousStatus = evnt.Status;
        evnt.Status = EventStatus.Draft;
        await _eventRepository.UpdateAsync(evnt);

        // Get admin user info
        var adminUser = await _identityService.GetByIdAsync(adminUserId);

        // Create approval history
        var history = new EventApprovalHistory
        {
            EventId = eventId,
            PreviousStatus = previousStatus,
            NewStatus = EventStatus.Draft,
            Comment = comment,
            ApprovedByUserId = adminUserId,
            ApprovedByUserName = adminUser?.Email ?? "Admin",
            ActionDate = DateTime.UtcNow
        };
        await _eventApprovalHistoryRepository.AddAsync(history);

        return new EventSubmissionResponseDto
        {
            Success = true,
            Message = "Event rejected successfully",
            NewStatus = EventStatus.Draft
        };
    }
}