using System.Data;
using System.Text.Json;
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

    public EventService(IEventRepository eventRepository, IMapper mapper, ICloudStorageService storageService, IUnitOfWork unitOfWork, IEventAddressRepository eventAddressRepository, IOrganizationRepository organizationRepository, IChartRepository chartRepository, IEventItemRepository eventItemRepository, IIdentityService identityService, ITicketClassRepository ticketClassRepository, ISeatService seatService, IEventSeatRepository eventSeatRepository, IEventSeatStateRepository eventSeatStateRepository, IEventApprovalHistoryRepository eventApprovalHistoryRepository, IMessageQueueService messageQueueService, IEventTaskRepository eventTaskRepository, ITaskOptionRepository taskOptionRepository)
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
        _eventTaskRepository = eventTaskRepository;
        _taskOptionRepository = taskOptionRepository;
    }

    private readonly IIdentityService _identityService;
    private readonly ITicketClassRepository _ticketClassRepository;
    private readonly ISeatService _seatService;
    private readonly IEventSeatRepository _eventSeatRepository;
    private readonly IEventSeatStateRepository _eventSeatStateRepository;
    private readonly IEventApprovalHistoryRepository _eventApprovalHistoryRepository;
    private readonly IMessageQueueService _messageQueueService;
    private readonly IEventTaskRepository _eventTaskRepository;
    private readonly ITaskOptionRepository _taskOptionRepository;



    public async Task<CreateEventResponse> CreateEventInfoAsync(Guid userId, CreateEventDto dto)
    {
        var isBanned = await _identityService.IsUserBannedAsync(userId);
        if (isBanned)
            throw new UnauthorizedAccessException("Your account has been banned. You cannot create events.");

        string? uploadedUrl = null;
        var org = await _organizationRepository.GetByIdAsync(dto.OrganizationId);
        if (org == null)
            throw new Exception("Organization not found");
        try
        {
            await _unitOfWork.BeginTransactionAsync();
            var evnt = _mapper.Map<Event>(dto);
            await _eventRepository.AddAsync(evnt);
            uploadedUrl = await _storageService.UploadAsync(dto.BannerStream, evnt.Id.ToString(), 1200, 600, "limit");
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
            
            // Create default task: Scan ticket with default option Gate A
            var defaultTask = new EventTask
            {
                EventId = evnt.Id,
                Name = "Scan ",
                Description = "Default task for scanning tickets",
                IsDefault = true
            };
            await _eventTaskRepository.AddAsync(defaultTask);
            
            // Create default option: Gate A
            var defaultOption = new TaskOption
            {
                TaskId = defaultTask.Id,
                OptionName = "Gate A"
            };
            await _taskOptionRepository.AddAsync(defaultOption);
            
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
                uploadedUrl = await _storageService.UploadAsync(dto.BannerStream, evnt.Id.ToString(), 1200, 600, "limit");
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
        
        if(!string.IsNullOrEmpty(dto.Name))
            ticketClass.Name = dto.Name;
        if(dto.Price!=null)
            ticketClass.Price = dto.Price.Value;
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
            if (dto.MaxPerUser.HasValue)
                eventItem.MaxPerUser = dto.MaxPerUser.Value;
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
        
        foreach (var ticketClass in ticketClasses)
        {
            if (!string.IsNullOrEmpty(ticketClass.Color))
            {
                await _ticketClassRepository.UpdateAsync(ticketClass);
            }
        }
        
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
        
        // Update chart key if provided (from Seats.io designer)
        bool chartKeyChanged = false;
        if (!string.IsNullOrEmpty(dto.ChartKey) && dto.ChartKey != chart.Key)
        {
            chart.Key = dto.ChartKey;
            chartKeyChanged = true;
        }
        
        // If chart key was updated, fetch venue definition and objects from Seats.io
        if (chartKeyChanged && !string.IsNullOrEmpty(chart.Key))
        {
            try
            {
                // Get venue definition from Seats.io
                var venueDefinition = await _seatService.GetVenueDefinitionFromChartAsync(chart.Key);
                if (!string.IsNullOrEmpty(venueDefinition))
                {
                    chart.VenueDefinition = venueDefinition;
                    
                    // Parse venue definition to extract objects and save to EventSeat
                    await ProcessChartVenueDefinitionAsync(chart, venueDefinition);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the update - venue definition can be fetched later
                // In production, you might want to log this to a logging service
                Console.WriteLine($"Warning: Failed to fetch venue definition for chart {chart.Key}: {ex.Message}");
            }
        }
        
        await _chartRepository.UpdateAsync(chart);
        return  _mapper.Map<ChartDto>(chart);
    }
    
    private async Task ProcessChartVenueDefinitionAsync(Chart chart, string venueDefinitionJson)
    {
        try
        {
            // Parse venue definition JSON
            using var doc = System.Text.Json.JsonDocument.Parse(venueDefinitionJson);
            var root = doc.RootElement;
            
            // Try to find objects in different possible locations
            JsonElement? objectsElement = null;
            
            if (root.TryGetProperty("objects", out var objectsDirect))
            {
                objectsElement = objectsDirect;
            }
            else if (root.TryGetProperty("venue", out var venueElement))
            {
                if (venueElement.TryGetProperty("objects", out var venueObjects))
                {
                    objectsElement = venueObjects;
                }
            }
            
            if (!objectsElement.HasValue || objectsElement.Value.ValueKind != JsonValueKind.Object)
            {
                // No objects found in venue definition
                return;
            }
            
            var eventSeats = new List<EventSeat>();
            
            // Extract seats from venue definition
            foreach (var objProperty in objectsElement.Value.EnumerateObject())
            {
                var obj = objProperty.Value;
                
                if (!obj.TryGetProperty("type", out var typeElement))
                    continue;
                
                var objType = typeElement.GetString();
                if (objType != "seat" && objType != "table")
                    continue;
                
                var seatLabel = obj.TryGetProperty("label", out var labelElement) 
                    ? labelElement.GetString() 
                    : objProperty.Name;
                
                if (string.IsNullOrEmpty(seatLabel))
                    continue;
                
                var seat = new EventSeat
                {
                    ChartId = chart.Id,
                    Label = seatLabel, // Label is now the unique identifier
                    Section = obj.TryGetProperty("section", out var sectionElement) 
                        ? sectionElement.GetString() 
                        : null,
                    Row = obj.TryGetProperty("row", out var rowElement) 
                        ? rowElement.GetString() 
                        : null,
                    Number = obj.TryGetProperty("number", out var numberElement) 
                        ? numberElement.GetString() 
                        : null,
                    CategoryKey = obj.TryGetProperty("categoryKey", out var categoryKeyElement) 
                        ? categoryKeyElement.GetString() 
                        : (obj.TryGetProperty("category", out var categoryElement) 
                            ? categoryElement.GetString() 
                            : null),
                    ExtraData = obj.TryGetProperty("extraData", out var extraDataElement) 
                        ? extraDataElement.GetRawText() 
                        : null
                };
                
                eventSeats.Add(seat);
            }
            
            // Bulk upsert seats
            if (eventSeats.Any())
            {
                await _eventSeatRepository.BulkUpsertAsync(eventSeats);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail - seats can be processed later by worker
            Console.WriteLine($"Warning: Failed to process venue definition for chart {chart.Id}: {ex.Message}");
        }
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

    public async Task<SyncSeatsResponseDto> SyncSeatsAsync(Guid userId, Guid eventId, Guid chartId, IEnumerable<SeatInfoDto> seatsData, string? venueDefinition = null, string? chartKey = null)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new Exception("Event not found");
        await ValidateEventOwnerAsync(userId, [evnt.OrganizationId]);

        var chart = await _chartRepository.GetByIdAsync(chartId);
        if (chart == null || chart.EventId != eventId)
            throw new Exception("Chart not found");

        // Update chart key from Seats.io designer if provided
        if (!string.IsNullOrEmpty(chartKey))
        {
            chart.Key = chartKey;
        }
        
        // Lưu venue definition vào chart nếu có
        if (!string.IsNullOrEmpty(venueDefinition))
        {
            chart.VenueDefinition = venueDefinition;
        }
        
        // Update chart if any changes
        if (!string.IsNullOrEmpty(chartKey) || !string.IsNullOrEmpty(venueDefinition))
        {
            await _chartRepository.UpdateAsync(chart);
        }

        // Convert seats data from frontend (from Seats.io designer) to EventSeat entities
        var eventSeats = seatsData.Select(s => 
        {
            var seatLabel = s.Label ?? s.SeatKey ?? throw new InvalidOperationException("Seat must have either Label or SeatKey");
            return new EventSeat
            {
                ChartId = chartId,
                Label = seatLabel, // Label is now the unique identifier
                Section = s.Section,
                Row = s.Row,
                Number = s.Number,
                CategoryKey = s.CategoryKey,
                ExtraData = s.ExtraData != null ? System.Text.Json.JsonSerializer.Serialize(s.ExtraData) : null
            };
        }).ToList();

        // Bulk upsert seats
        var existingSeats = await _eventSeatRepository.GetByChartIdAsync(chartId);
        var existingLabels = existingSeats.Select(s => s.Label).ToHashSet();
        
        int newSeats = eventSeats.Count(s => !existingLabels.Contains(s.Label));
        
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
                    SeatKey = seat.Label, // Map Label to SeatKey for backward compatibility
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
                    SeatKey = seat.Label, // Map Label to SeatKey for backward compatibility
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
            ApprovedByUserId = null, 
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

        var history = new EventApprovalHistory
        {
            EventId = eventId,
            PreviousStatus = previousStatus,
            NewStatus = EventStatus.Draft,
            Comment = "Event cancelled by organizer",
            ApprovedByUserId = null, 
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

    public async Task<CustomerEventListDto> GetPublishedEventsAsync()
    {
        var now = DateTime.UtcNow;
        var publishedEvents = await _eventRepository.GetAllAsync(1, int.MaxValue, EventStatus.Published);
        
        var upcomingEvents = new List<CustomerEventDto>();
        var pastEvents = new List<CustomerEventDto>();
        
        foreach (var evnt in publishedEvents.Data)
        {
            var eventItems = await _eventItemRepository.GetAllByEventIdAsync(evnt.Id);
            if (!eventItems.Any()) continue;
            
            var earliestStart = eventItems.Min(ei => ei.Start);
            
            var ticketClasses = await _ticketClassRepository.GetEventTicketClassesAsync(evnt.Id);
            decimal? lowestPrice = ticketClasses.Any() 
                ? ticketClasses.Min(tc => tc.Price) 
                : null;
            
            var customerEvent = new CustomerEventDto
            {
                Id = evnt.Id,
                Name = evnt.Name,
                Banner = evnt.Banner,
                Start = earliestStart,
                LowestPrice = lowestPrice,
                OrganizationName = evnt.Organization?.Name ?? "Unknown",
                ProvinceName = evnt.EventAddress?.ProvinceName
            };
            
            if (earliestStart >= now)
            {
                upcomingEvents.Add(customerEvent);
            }
            else
            {
                pastEvents.Add(customerEvent);
            }
        }
        
        upcomingEvents = upcomingEvents.OrderBy(e => e.Start).ToList();
        pastEvents = pastEvents.OrderByDescending(e => e.Start).ToList();
        
        return new CustomerEventListDto
        {
            UpcomingEvents = upcomingEvents,
            PastEvents = pastEvents
        };
    }

    public async Task<PaginatedResult<CustomerEventDto>> GetEventsAsync(string? search = null, int page = 1, int size = 10, string? timeSort = null, string? province = null, string? eventType = null)
    {
        var eventsResult = await _eventRepository.GetEventsAsync(search, page, size, timeSort, province, eventType);
        
        var customerEvents = new List<CustomerEventDto>();
        
        foreach (var evnt in eventsResult.Data)
        {
            var eventItems = evnt.EventItem?.ToList() ?? new List<EventItem>();
            if (!eventItems.Any()) continue;
            
            var earliestStart = eventItems.Min(ei => ei.Start);
            
            var ticketClasses = await _ticketClassRepository.GetEventTicketClassesAsync(evnt.Id);
            decimal? lowestPrice = ticketClasses.Any() 
                ? ticketClasses.Min(tc => tc.Price) 
                : null;
            
            var customerEvent = new CustomerEventDto
            {
                Id = evnt.Id,
                Name = evnt.Name,
                Banner = evnt.Banner,
                Start = earliestStart,
                LowestPrice = lowestPrice,
                OrganizationName = evnt.Organization?.Name ?? "Unknown",
                ProvinceName = evnt.EventAddress?.ProvinceName
            };
            
            customerEvents.Add(customerEvent);
        }
        
        return new PaginatedResult<CustomerEventDto>
        {
            Data = customerEvents,
            Total = eventsResult.Total,
            Page = eventsResult.Page,
            Size = eventsResult.Size
        };
    }

    public async Task<CustomerEventDetailDto> GetPublishedEventDetailAsync(Guid eventId)
    {
        var evnt = await _eventRepository.GetDetailEventAsync(eventId);
        if (evnt == null)
            throw new KeyNotFoundException("Event not found");
        
        if (evnt.Status != EventStatus.Published)
            throw new UnauthorizedAccessException("Event is not published");
        
        var eventItems = await _eventItemRepository.GetAllByEventIdAsync(eventId);
        var ticketClasses = await _ticketClassRepository.GetEventTicketClassesAsync(eventId);
        
        var customerTicketClasses = ticketClasses.Select(tc => new CustomerTicketClassDto
        {
            Id = tc.Id,
            Name = tc.Name,
            Price = tc.Price,
            Color = tc.Color
        }).ToList();
        
        var customerEventItems = new List<CustomerEventItemDto>();
        
        foreach (var item in eventItems)
        {
            customerEventItems.Add(new CustomerEventItemDto
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Start = item.Start,
                End = item.End
            });
        }
        
        var earliestStart = eventItems.Any() 
            ? eventItems.Min(ei => ei.Start) 
            : evnt.Start;
        
        return new CustomerEventDetailDto
        {
            Id = evnt.Id,
            Name = evnt.Name,
            Description = evnt.Description,
            Banner = evnt.Banner,
            Start = earliestStart,
            EventAddress = _mapper.Map<EventAddressDto>(evnt.EventAddress),
            OrganizationName = evnt.Organization?.Name ?? "Unknown",
            TicketClasses = customerTicketClasses,
            EventItems = customerEventItems
        };
    }

    public async Task<CustomerSeatMapDto> GetEventItemSeatMapAsync(Guid eventId, Guid eventItemId)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new KeyNotFoundException("Event not found");
        
        if (evnt.Status != EventStatus.Published)
            throw new UnauthorizedAccessException("Event is not published");
        
        var eventItem = await _eventItemRepository.GetByIdAsync(eventItemId);
        if (eventItem == null)
            throw new KeyNotFoundException("EventItem not found");
        
        if (eventItem.EventId != eventId)
            throw new InvalidOperationException("EventItem does not belong to this event");
        
        var chart = await _chartRepository.GetByIdAsync(eventItem.ChartId);
        if (chart == null)
            throw new KeyNotFoundException("Chart not found");
        
        if (string.IsNullOrEmpty(chart.Key))
            throw new InvalidOperationException("Chart key is not configured. Please configure the seat map first.");
        
        var eventSeats = await _eventSeatRepository.GetByChartIdAsync(chart.Id);
        
        var seatStates = await _eventSeatStateRepository.GetByEventItemIdAsync(eventItemId);
        var seatStateDict = seatStates.ToDictionary(ss => ss.EventSeatId, ss => ss);
        
        var ticketClasses = await _ticketClassRepository.GetEventTicketClassesAsync(eventId);
        
        var defaultPrice = ticketClasses.Any() ? ticketClasses.Min(tc => tc.Price) : (decimal?)null;
        
        var customerSeats = new List<CustomerSeatDto>();
        
        foreach (var seat in eventSeats)
        {
            var seatState = seatStateDict.GetValueOrDefault(seat.Id);
            var status = seatState?.Status == SeatStatus.Paid ? "paid" : "free";
            
            decimal? price = defaultPrice;
            if (!string.IsNullOrEmpty(seat.CategoryKey))
            {
                price = defaultPrice;
            }
            
            customerSeats.Add(new CustomerSeatDto
            {
                EventSeatId = seat.Id,
                SeatKey = seat.Label, // Map Label to SeatKey for backward compatibility
                Label = seat.Label,
                Section = seat.Section,
                Row = seat.Row,
                Number = seat.Number,
                CategoryKey = seat.CategoryKey,
                Status = status,
                Price = price
            });
        }
        
        var customerTicketClasses = ticketClasses.Select(tc => new CustomerTicketClassDto
        {
            Id = tc.Id,
            Name = tc.Name,
            Price = tc.Price,
            Color = tc.Color
        }).ToList();
        
        return new CustomerSeatMapDto
        {
            EventItemId = eventItem.Id,
            EventItemName = eventItem.Name,
            ChartId = chart.Id,
            ChartKey = chart.Key,
            EventKey = eventItem.EventKey, // Seats.io event key (preferred)
            ChartName = chart.Name,
            VenueDefinition = chart.VenueDefinition,
            MaxPerUser = eventItem.MaxPerUser,
            Seats = customerSeats,
            TicketClasses = customerTicketClasses
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

        var eventItems = await _eventItemRepository.GetAllByEventIdAsync(eventId);
        var createdEventKeys = new List<string>();
        var failedEventItems = new List<string>();
        var eventItemsWithValidKeys = new List<Guid>();

        foreach (var eventItem in eventItems)
        {
            try
            {
                if (!string.IsNullOrEmpty(eventItem.EventKey))
                {
                    try
                    {
                        // Verify event key exists in Seats.io
                        await _seatService.RetrieveEventAsync(eventItem.EventKey);
                        eventItemsWithValidKeys.Add(eventItem.Id);
                        continue;
                    }
                    catch
                    {
                        eventItem.EventKey = null;
                    }
                }

                var chart = await _chartRepository.GetByIdAsync(eventItem.ChartId);
                if (chart == null)
                {
                    failedEventItems.Add($"{eventItem.Name} (Chart not found)");
                    continue;
                }

                if (string.IsNullOrEmpty(chart.Key))
                {
                    failedEventItems.Add($"{eventItem.Name} (Chart key is not configured)");
                    continue;
                }

                var eventKey = await CreateEventKeyForEventItemInternalAsync(eventItem, chart);
                createdEventKeys.Add(eventKey);
                eventItemsWithValidKeys.Add(eventItem.Id);
            }
            catch (Exception ex)
            {
                // Log error but continue with other eventItems
                failedEventItems.Add($"{eventItem.Name} ({ex.Message})");
            }
        }

        var message = new EventProcessingMessage
        {
            EventId = eventId,
            AdminUserId = adminUserId,
            RequestedAt = DateTime.UtcNow
        };
        await _messageQueueService.PublishEventProcessingMessageAsync(message);
        // Logging is handled by InMemoryMessageQueueService

        var messageText = "Event approval initiated.";
        if (createdEventKeys.Count > 0)
        {
            messageText += $" Created {createdEventKeys.Count} event key(s) for event items.";
        }
        if (eventItemsWithValidKeys.Count > 0)
        {
            messageText += $" {eventItemsWithValidKeys.Count} event item(s) will be processed by background worker to fetch seats.";
        }
        if (failedEventItems.Count > 0)
        {
            messageText += $" Failed to create event keys for {failedEventItems.Count} event item(s): {string.Join(", ", failedEventItems)}.";
        }

        return new EventSubmissionResponseDto
        {
            Success = true,
            Message = messageText,
            NewStatus = EventStatus.InProgress
        };
    }

    private async Task<string> CreateEventKeyForEventItemInternalAsync(EventItem eventItem, Chart chart)
    {
        // Check if event key already exists
        if (!string.IsNullOrEmpty(eventItem.EventKey))
            return eventItem.EventKey;
        
        if (string.IsNullOrEmpty(chart.Key))
            throw new InvalidOperationException("Chart key is not configured. Please configure the seat map first.");
        
        try
        {
            await _seatService.PublishDraftVersionAsync(chart.Key);
            
            var eventKey = $"event-{eventItem.Id}";
            
            var seatsIoEventKey = await _seatService.CreateEventFromChartAsync(chart.Key, eventKey);
            
            // Save event key to eventItem
            eventItem.EventKey = seatsIoEventKey;
            await _eventItemRepository.UpdateAsync(eventItem);
            
            return seatsIoEventKey;
        }
        catch (Exception ex)
        {
            // If Seats.io API returns error, it means chart key is invalid
            throw new Exception($"Failed to create Seats.io event key for eventItem {eventItem.Id}. Chart key may be invalid or chart does not exist in Seats.io: {ex.Message}", ex);
        }
    }

    public async Task<string> CreateEventKeyForEventItemAsync(Guid userId, Guid eventId, Guid eventItemId)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new KeyNotFoundException("Event not found");
        
        if (evnt.Status != EventStatus.Published)
            throw new InvalidOperationException($"Cannot create event key for event with status {evnt.Status}. Event must be published.");
        
        var eventItem = await _eventItemRepository.GetByIdAsync(eventItemId);
        if (eventItem == null)
            throw new KeyNotFoundException("EventItem not found");
        
        if (eventItem.EventId != eventId)
            throw new InvalidOperationException("EventItem does not belong to this event");
        
        var chart = await _chartRepository.GetByIdAsync(eventItem.ChartId);
        if (chart == null)
            throw new KeyNotFoundException("Chart not found");
        
        return await CreateEventKeyForEventItemInternalAsync(eventItem, chart);
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

        var adminUser = await _identityService.GetByIdAsync(adminUserId);

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

    public async Task<bool> ReProcessEventItemSeatsAsync(Guid userId, Guid eventId, Guid eventItemId)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new KeyNotFoundException("Event not found");

        await ValidateEventOwnerAsync(userId, [evnt.OrganizationId]);

        var eventItem = await _eventItemRepository.GetByIdAsync(eventItemId);
        if (eventItem == null)
            throw new KeyNotFoundException("EventItem not found");

        if (eventItem.EventId != eventId)
            throw new InvalidOperationException("EventItem does not belong to this event");

        var chart = await _chartRepository.GetByIdAsync(eventItem.ChartId);
        if (chart == null)
            throw new KeyNotFoundException("Chart not found");

        try
        {
            if (!string.IsNullOrEmpty(eventItem.EventKey))
            {
                var seats = await _seatService.GetAllObjectsFromEventAsync(eventItem.EventKey);
                if (seats.Any())
                {
                    await ProcessEventItemSeatsFromEventAsync(eventItem, seats);
                    return true;
                }
            }

            if (!string.IsNullOrEmpty(chart.VenueDefinition))
            {
                await ProcessEventItemSeatsFromChartAsync(eventItem, chart);
                return true;
            }
            else if (!string.IsNullOrEmpty(chart.Key))
            {
                var venueDefinition = await _seatService.GetVenueDefinitionFromChartAsync(chart.Key);
                if (!string.IsNullOrEmpty(venueDefinition))
                {
                    chart.VenueDefinition = venueDefinition;
                    await _chartRepository.UpdateAsync(chart);
                    await ProcessEventItemSeatsFromChartAsync(eventItem, chart);
                    return true;
                }
            }

            throw new InvalidOperationException("Cannot fetch seats: no eventKey or chart venue definition available");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to re-process event item seats: {ex.Message}", ex);
        }
    }

    private async Task ProcessEventItemSeatsFromEventAsync(EventItem eventItem, IEnumerable<SeatInfoDto> seats)
    {
        var eventSeats = new List<EventSeat>();

        foreach (var seatDto in seats)
        {
            var seatLabel = seatDto.Label ?? seatDto.SeatKey ?? throw new InvalidOperationException("Seat must have either Label or SeatKey");
            var existingSeat = await _eventSeatRepository.GetSeatByLabelAsync(eventItem.ChartId, seatLabel);
            
            if (existingSeat == null)
            {
                var seat = new EventSeat
                {
                    ChartId = eventItem.ChartId,
                    Label = seatLabel, // Label is now the unique identifier
                    Section = seatDto.Section,
                    Row = seatDto.Row,
                    Number = seatDto.Number,
                    CategoryKey = seatDto.CategoryKey,
                    ExtraData = seatDto.ExtraData != null 
                        ? System.Text.Json.JsonSerializer.Serialize(seatDto.ExtraData) 
                        : null
                };
                eventSeats.Add(seat);
            }
            else
            {
                existingSeat.Label = seatDto.Label ?? existingSeat.Label;
                existingSeat.Section = seatDto.Section ?? existingSeat.Section;
                existingSeat.Row = seatDto.Row ?? existingSeat.Row;
                existingSeat.Number = seatDto.Number ?? existingSeat.Number;
                existingSeat.CategoryKey = seatDto.CategoryKey ?? existingSeat.CategoryKey;
                if (seatDto.ExtraData != null)
                {
                    existingSeat.ExtraData = System.Text.Json.JsonSerializer.Serialize(seatDto.ExtraData);
                }
                eventSeats.Add(existingSeat);
            }
        }

        if (eventSeats.Any())
        {
            await _eventSeatRepository.BulkUpsertAsync(eventSeats);
        }

        var allSeats = await _eventSeatRepository.GetByChartIdAsync(eventItem.ChartId);
        foreach (var seat in allSeats)
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

    private async Task ProcessEventItemSeatsFromChartAsync(EventItem eventItem, Chart chart)
    {
        if (string.IsNullOrEmpty(chart.VenueDefinition))
            throw new InvalidOperationException("Chart venue definition is not available");

        using var doc = System.Text.Json.JsonDocument.Parse(chart.VenueDefinition);
        var root = doc.RootElement;

        if (!root.TryGetProperty("objects", out var objectsElement))
            throw new InvalidOperationException("Invalid venue definition: missing 'objects'");

        var eventSeats = new List<EventSeat>();

        foreach (var obj in objectsElement.EnumerateArray())
        {
            if (!obj.TryGetProperty("id", out var idElement))
                continue;

            var label = obj.TryGetProperty("label", out var labelElement) 
                ? labelElement.GetString() 
                : idElement.GetString();
            
            if (string.IsNullOrEmpty(label))
                continue;

            var existingSeat = await _eventSeatRepository.GetSeatByLabelAsync(chart.Id, label);
            if (existingSeat == null)
            {
                var seat = new EventSeat
                {
                    ChartId = chart.Id,
                    Label = label, // Label is now the unique identifier
                    Section = obj.TryGetProperty("section", out var sectionElement) ? sectionElement.GetString() : null,
                    Row = obj.TryGetProperty("row", out var rowElement) ? rowElement.GetString() : null,
                    Number = obj.TryGetProperty("number", out var numberElement) ? numberElement.GetString() : null,
                    CategoryKey = obj.TryGetProperty("category", out var categoryElement) 
                        ? categoryElement.GetString() 
                        : null,
                    ExtraData = obj.TryGetProperty("extraData", out var extraDataElement) 
                        ? extraDataElement.GetRawText() 
                        : null
                };
                eventSeats.Add(seat);
            }
        }

        if (eventSeats.Any())
        {
            await _eventSeatRepository.BulkUpsertAsync(eventSeats);
        }

        var allSeats = await _eventSeatRepository.GetByChartIdAsync(chart.Id);
        foreach (var seat in allSeats)
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
}