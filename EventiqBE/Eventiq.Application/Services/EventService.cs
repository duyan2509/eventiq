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

    public EventService(IEventRepository eventRepository, IMapper mapper, ICloudStorageService storageService, IUnitOfWork unitOfWork, IEventAddressRepository eventAddressRepository, IOrganizationRepository organizationRepository, IChartRepository chartRepository, IEventItemRepository eventItemRepository, IIdentityService identityService, ITicketClassRepository ticketClassRepository, ISeatService seatService)
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
    }

    private readonly IIdentityService _identityService;
    private readonly ITicketClassRepository _ticketClassRepository;
    private readonly ISeatService _seatService;



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

    public async Task<IEnumerable<ChartDto>> GetEventChartAsync(Guid userId, Guid eventId)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new Exception("Event not found");
        await ValidateEventOwnerAsync(userId, [evnt.OrganizationId]);

        var charts = await _chartRepository.GetByEventItAsync(eventId);
        return charts.Select(chart => _mapper.Map<ChartDto>(chart));
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
}