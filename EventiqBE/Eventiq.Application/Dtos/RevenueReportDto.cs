namespace Eventiq.Application.Dtos;

public class AdminRevenueReportDto
{
    public decimal TotalMonthlyRevenue { get; set; }
    public decimal TotalPlatformFee { get; set; }
    public decimal TotalOrgAmount { get; set; }
    public int TotalTicketsSold { get; set; }
    public int PendingPayoutEventsCount { get; set; } 
    public List<EventItemRevenueDto> EventItems { get; set; } = new();
    public List<EventRevenueDto> Events { get; set; } = new(); 
}
public class EventItemRevenueDto
{
    public Guid EventItemId { get; set; }
    public string EventItemName { get; set; } = string.Empty;
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int TicketsSold { get; set; }
    public decimal GrossRevenue { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal OrgAmount { get; set; }
    public string PayoutStatus { get; set; } = "PENDING";
    public DateTime? PaidAt { get; set; }
}

public class EventRevenueDto
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int TicketsSold { get; set; }
    public int TotalTickets { get; set; }
    public decimal PlatformFee { get; set; }
}

public class OrgRevenueReportDto
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public List<OrgEventItemReportDto> EventItems { get; set; } = new();
    public decimal TotalGrossRevenue { get; set; }
    public decimal TotalOrgAmount { get; set; }
    public int TotalTicketsSold { get; set; }
}

public class OrgRevenueStatsDto
{
    public int TotalSoldTickets { get; set; }
    public decimal TotalGrossRevenue { get; set; }
    public decimal TotalOrganizationAmount { get; set; }
    public string PayoutStatus { get; set; } = "PENDING";
    public DateTime? PayoutDate { get; set; }
    public string? ProofImageUrl { get; set; }
    public List<TicketClassRevenueDto> TicketClassRevenues { get; set; } = new();
}

public class TicketClassRevenueDto
{
    public Guid TicketClassId { get; set; }
    public string TicketClassName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int TicketsSold { get; set; }
}

public class OrgRevenueTableDto
{
    public Guid EventItemId { get; set; }
    public string EventItemName { get; set; } = string.Empty;
    public Guid TicketClassId { get; set; }
    public string TicketClassName { get; set; } = string.Empty;
    public int TotalTickets { get; set; }
    public int SoldTickets { get; set; }
    public decimal GrossRevenue { get; set; }
    public decimal OrganizationAmount { get; set; }
}

public class OrgEventItemReportDto
{
    public Guid EventItemId { get; set; }
    public string EventItemName { get; set; } = string.Empty;
    public int TotalTickets { get; set; }
    public int TicketsSold { get; set; }
    public int TicketsRemaining { get; set; }
    public decimal GrossRevenue { get; set; }
    public decimal OrgAmount { get; set; }
    public string PayoutStatus { get; set; } = "PENDING";
    public string? ProofImageUrl { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class UserTicketDto
{
    public Guid TicketId { get; set; }
    public Guid EventItemId { get; set; }
    public string EventItemName { get; set; } = string.Empty;
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string TicketClassName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string SeatLabel { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public DateTime EventStartDate { get; set; }
    public DateTime EventEndDate { get; set; }
    public string Status { get; set; } = "UPCOMING"; // UPCOMING, EXPIRED
}

