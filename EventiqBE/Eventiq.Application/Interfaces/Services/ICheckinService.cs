using Eventiq.Application.Dtos;

namespace Eventiq.Application.Interfaces.Services;

public interface ICheckinService
{
    Task<PaginatedResult<CheckinDto>> GetCheckinsAsync(Guid userId, Guid eventId, Guid eventItemId, int page = 1, int size = 10);
    Task<CheckinDto> CheckinTicketAsync(Guid userId, Guid eventItemId, CheckinTicketRequest request);
    Task<CheckInRequestResponseDto> RequestCheckInAsync(Guid userId, Guid ticketId, CheckInRequestDto request);
    Task<StaffCheckInResponseDto> StaffCheckInAsync(Guid staffUserId, StaffCheckInRequestDto request);
}
