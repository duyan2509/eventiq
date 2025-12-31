namespace Eventiq.Domain.Entities;

public class Payout : BaseEntity
{
    public Guid EventId { get; set; }
    public virtual Event Event { get; set; }
    
    public Guid EventItemId { get; set; }
    public virtual EventItem EventItem { get; set; }
    
    public Guid OrganizationId { get; set; }
    public virtual Organization Organization { get; set; }
    
    // Revenue summary
    public decimal GrossRevenue { get; set; } 
    public decimal PlatformFee { get; set; } 
    public decimal OrgAmount { get; set; }
    
    // Payout status
    public PayoutStatus Status { get; set; } = PayoutStatus.PENDING;
    
    // Manual payout info
    public string? ProofImageUrl { get; set; } 
    public string? Notes { get; set; }
    public DateTime? PaidAt { get; set; } 
    public string? PaidByUserId { get; set; } 
}

public enum PayoutStatus
{
    PENDING,    
    PAID        
}

