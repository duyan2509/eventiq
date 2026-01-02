using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Application.Interfaces.Services;
using Eventiq.Domain.Entities;

namespace Eventiq.Application.Services;

public class CheckinService : ICheckinService
{
    private readonly ICheckinRepository _checkinRepository;
    private readonly IEventItemRepository _eventItemRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly IIdentityService _identityService;
    private readonly IStaffRepository _staffRepository;

    public CheckinService(
        ICheckinRepository checkinRepository,
        IEventItemRepository eventItemRepository,
        IEventRepository eventRepository,
        ITicketRepository ticketRepository,
        IIdentityService identityService,
        IStaffRepository staffRepository)
    {
        _checkinRepository = checkinRepository;
        _eventItemRepository = eventItemRepository;
        _eventRepository = eventRepository;
        _ticketRepository = ticketRepository;
        _identityService = identityService;
        _staffRepository = staffRepository;
    }

    public async Task<PaginatedResult<CheckinDto>> GetCheckinsAsync(Guid userId, Guid eventId, Guid eventItemId, int page = 1, int size = 10)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new KeyNotFoundException("Event not found");

        var eventItem = await _eventItemRepository.GetByDetailByIdAsync(eventItemId);
        if (eventItem == null || eventItem.EventId != eventId)
            throw new KeyNotFoundException("Event item not found");

        var userOrgs = await _identityService.GetUserOrgsAsync(userId);
        var isOrgOwner = userOrgs.Contains(evnt.OrganizationId);
        var isStaff = await _staffRepository.GetByEventIdAndUserIdAsync(eventId, userId) != null;

        if (!isOrgOwner && !isStaff)
            throw new UnauthorizedAccessException("User does not have permission to view checkins");

        var paginatedCheckins = await _checkinRepository.GetByEventItemIdPaginatedAsync(eventItemId, page, size);
        var checkinDtos = new List<CheckinDto>();

        foreach (var checkin in paginatedCheckins.Data)
        {
            var staff = checkin.Staff;
            var ticket = checkin.Ticket;
            UserDto? customer = null;
            if (Guid.TryParse(ticket.UserId, out var customerUserId))
            {
                customer = await _identityService.GetByIdAsync(customerUserId);
            }

            checkinDtos.Add(new CheckinDto
            {
                Id = checkin.Id,
                TicketId = checkin.TicketId,
                EventItemId = checkin.EventItemId,
                EventItemName = eventItem.Name,
                StaffId = checkin.StaffId,
                StaffName = staff?.UserId.ToString() ?? string.Empty,
                CustomerId = ticket.UserId,
                CustomerName = customer?.Username ?? string.Empty,
                CheckinTime = checkin.CheckinTime
            });
        }

        return new PaginatedResult<CheckinDto>
        {
            Data = checkinDtos,
            Total = paginatedCheckins.Total,
            Page = paginatedCheckins.Page,
            Size = paginatedCheckins.Size
        };
    }

    public async Task<CheckinDto> CheckinTicketAsync(Guid userId, Guid eventItemId, CheckinTicketRequest request)
    {
        var eventItem = await _eventItemRepository.GetByDetailByIdAsync(eventItemId);
        if (eventItem == null)
            throw new KeyNotFoundException("Event item not found");

        var evnt = await _eventRepository.GetByIdAsync(eventItem.EventId);
        if (evnt == null)
            throw new KeyNotFoundException("Event not found");

        var userOrgs = await _identityService.GetUserOrgsAsync(userId);
        var isOrgOwner = userOrgs.Contains(evnt.OrganizationId);
        var staff = await _staffRepository.GetByEventIdAndUserIdAsync(eventItem.EventId, userId);

        if (!isOrgOwner && staff == null)
            throw new UnauthorizedAccessException("User does not have permission to checkin tickets");

        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId);
        if (ticket == null || ticket.IsDeleted)
            throw new KeyNotFoundException("Ticket not found");

        if (ticket.EventItemId != eventItemId)
            throw new InvalidOperationException("Ticket does not belong to this event item");

        return new CheckinDto
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            EventItemId = eventItemId,
            EventItemName = eventItem.Name,
            StaffId = staff?.Id ?? Guid.Empty,
            StaffName = staff?.UserId.ToString() ?? string.Empty,
            CustomerId = ticket.UserId,
            CustomerName = string.Empty,
            CheckinTime = DateTime.UtcNow
        };
    }
}
