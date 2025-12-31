namespace Eventiq.Application.Dtos;

public class PayoutDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid EventItemId { get; set; }
    public Guid OrganizationId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventItemName { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public decimal GrossRevenue { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal OrgAmount { get; set; }
    public string Status { get; set; } = "PENDING";
    public string? ProofImageUrl { get; set; }
    public string? Notes { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaidByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    // Banking info from Event
    public string? BankCode { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountName { get; set; }
}

public class UpdatePayoutDto
{
    public string? ProofImageUrl { get; set; }
    public string? Notes { get; set; }
}

