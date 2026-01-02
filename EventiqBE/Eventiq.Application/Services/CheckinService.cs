using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces;
using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Application.Interfaces.Services;
using Eventiq.Domain.Entities;
using System.Security.Cryptography;
using System.Text;

namespace Eventiq.Application.Services;

public class CheckinService : ICheckinService
{
    private readonly ICheckinRepository _checkinRepository;
    private readonly IEventItemRepository _eventItemRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly IIdentityService _identityService;
    private readonly IStaffRepository _staffRepository;
    private readonly IVerifyRequestRepository _verifyRequestRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CheckinService(
        ICheckinRepository checkinRepository,
        IEventItemRepository eventItemRepository,
        IEventRepository eventRepository,
        ITicketRepository ticketRepository,
        IIdentityService identityService,
        IStaffRepository staffRepository,
        IVerifyRequestRepository verifyRequestRepository,
        IUnitOfWork unitOfWork)
    {
        _checkinRepository = checkinRepository;
        _eventItemRepository = eventItemRepository;
        _eventRepository = eventRepository;
        _ticketRepository = ticketRepository;
        _identityService = identityService;
        _staffRepository = staffRepository;
        _verifyRequestRepository = verifyRequestRepository;
        _unitOfWork = unitOfWork;
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
            string customerNameOrEmail = string.Empty;
            if (Guid.TryParse(ticket.UserId, out var customerUserId))
            {
                customer = await _identityService.GetByIdAsync(customerUserId);
                customerNameOrEmail = !string.IsNullOrEmpty(customer?.Username) ? customer.Username : customer?.Email ?? string.Empty;
            }

            UserDto? staffUser = null;
            string staffNameOrEmail = string.Empty;
            if (staff != null)
            {
                staffUser = await _identityService.GetByIdAsync(staff.UserId);
                staffNameOrEmail = !string.IsNullOrEmpty(staffUser?.Username) ? staffUser.Username : staffUser?.Email ?? string.Empty;
            }

            checkinDtos.Add(new CheckinDto
            {
                Id = checkin.Id,
                CustomerNameOrEmail = customerNameOrEmail,
                StaffNameOrEmail = staffNameOrEmail,
                EventItemName = eventItem.Name,
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

        UserDto? customer = null;
        string customerNameOrEmail = string.Empty;
        if (Guid.TryParse(ticket.UserId, out var customerUserId))
        {
            customer = await _identityService.GetByIdAsync(customerUserId);
            customerNameOrEmail = !string.IsNullOrEmpty(customer?.Username) ? customer.Username : customer?.Email ?? string.Empty;
        }

        UserDto? staffUser = null;
        string staffNameOrEmail = string.Empty;
        if (staff != null)
        {
            staffUser = await _identityService.GetByIdAsync(staff.UserId);
            staffNameOrEmail = !string.IsNullOrEmpty(staffUser?.Username) ? staffUser.Username : staffUser?.Email ?? string.Empty;
        }

        return new CheckinDto
        {
            Id = Guid.NewGuid(),
            CustomerNameOrEmail = customerNameOrEmail,
            StaffNameOrEmail = staffNameOrEmail,
            EventItemName = eventItem.Name,
            CheckinTime = DateTime.UtcNow
        };
    }

    public async Task<CheckInRequestResponseDto> RequestCheckInAsync(Guid userId, Guid ticketId, CheckInRequestDto request)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        if (ticket == null || ticket.IsDeleted)
            throw new KeyNotFoundException("Ticket not found");

        if (ticket.UserId != userId.ToString())
            throw new UnauthorizedAccessException("Ticket does not belong to this user");

        if (ticket.Status == TicketStatus.USED)
            throw new InvalidOperationException("Ticket has already been used");

        var isPasswordValid = await _identityService.VerifyPasswordAsync(userId, request.Password);
        if (!isPasswordValid)
            throw new UnauthorizedAccessException("Invalid password");

        var otp = GenerateOtp();
        var otpHash = HashOtp(otp);
        var expiresAt = DateTime.UtcNow.AddSeconds(90);

        var existingRequest = await _verifyRequestRepository.GetByTicketIdAsync(ticketId);
        if (existingRequest != null && existingRequest.Status == VerifyRequestStatus.OWNER_VERIFIED && existingRequest.ExpiresAt > DateTime.UtcNow)
        {
            throw new InvalidOperationException("Check-in request already exists for this ticket");
        }

        var verifyRequest = new VerifyRequest
        {
            TicketId = ticketId,
            OtpHash = otpHash,
            Status = VerifyRequestStatus.OWNER_VERIFIED,
            ExpiresAt = expiresAt
        };

        await _verifyRequestRepository.AddAsync(verifyRequest);

        return new CheckInRequestResponseDto
        {
            TicketCode = ticket.TicketCode,
            Otp = otp
        };
    }

    public async Task<StaffCheckInResponseDto> StaffCheckInAsync(Guid staffUserId, StaffCheckInRequestDto request)
    {
        var ticket = await _ticketRepository.GetByTicketCodeAsync(request.TicketCode);
        if (ticket == null || ticket.IsDeleted)
            throw new KeyNotFoundException("Ticket not found");

        if (ticket.Status == TicketStatus.USED)
            throw new InvalidOperationException("Ticket has already been used");

        var verifyRequest = await _verifyRequestRepository.GetByTicketIdAndStatusAsync(ticket.Id, VerifyRequestStatus.OWNER_VERIFIED);
        if (verifyRequest == null)
            throw new KeyNotFoundException("Verify request not found");

        if (verifyRequest.ExpiresAt < DateTime.UtcNow)
        {
            verifyRequest.Status = VerifyRequestStatus.EXPIRED;
            await _verifyRequestRepository.UpdateAsync(verifyRequest);
            throw new InvalidOperationException("OTP has expired");
        }

        var providedOtpHash = HashOtp(request.Otp);
        if (verifyRequest.OtpHash != providedOtpHash)
            throw new UnauthorizedAccessException("Invalid OTP");

        var eventItem = await _eventItemRepository.GetByDetailByIdAsync(ticket.EventItemId);
        if (eventItem == null)
            throw new KeyNotFoundException("Event item not found");

        var staff = await _staffRepository.GetByEventIdAndUserIdAsync(eventItem.EventId, staffUserId);
        if (staff == null)
            throw new UnauthorizedAccessException("User is not a staff member for this event");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            ticket.Status = TicketStatus.USED;
            await _ticketRepository.UpdateAsync(ticket);

            verifyRequest.Status = VerifyRequestStatus.SUCCESS;
            verifyRequest.VerifiedBy = staffUserId;
            await _verifyRequestRepository.UpdateAsync(verifyRequest);

            var checkin = new Checkin
            {
                TicketId = ticket.Id,
                EventItemId = ticket.EventItemId,
                StaffId = staff.Id,
                CheckinTime = DateTime.UtcNow
            };
            await _checkinRepository.AddAsync(checkin);

            await _unitOfWork.CommitAsync();

            return new StaffCheckInResponseDto
            {
                Success = true,
                Message = "Ticket checked in successfully",
                TicketId = ticket.Id,
                CustomerUserId = ticket.UserId
            };
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    private string GenerateOtp()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    private string HashOtp(string otp)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(otp);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
