namespace Eventiq.Application.Dtos;

public class CheckinDto
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Guid EventItemId { get; set; }
    public string EventItemName { get; set; } = string.Empty;
    public Guid StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
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
