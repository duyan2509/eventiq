namespace Eventiq.Application.Dtos;

public class CheckinDto
{
    public Guid Id { get; set; }
    public string CustomerNameOrEmail { get; set; } = string.Empty;
    public string StaffNameOrEmail { get; set; } = string.Empty;
    public string EventItemName { get; set; } = string.Empty;
    public DateTime CheckinTime { get; set; }
}

public class CheckinListDto
{
    public Guid EventId { get; set; }
    public Guid EventItemId { get; set; }
    public string EventItemName { get; set; } = string.Empty;
    public List<CheckinDto> Checkins { get; set; } = new();
}

public class CheckinTicketRequest
{
    public Guid TicketId { get; set; }
}

public class CheckInRequestDto
{
    public string Password { get; set; } = string.Empty;
}

public class CheckInRequestResponseDto
{
    public string TicketCode { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
}

public class StaffCheckInRequestDto
{
    public string TicketCode { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
}

public class StaffCheckInResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid TicketId { get; set; }
    public string CustomerUserId { get; set; } = string.Empty;
}