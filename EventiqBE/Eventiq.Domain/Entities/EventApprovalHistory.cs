namespace Eventiq.Domain.Entities;

public class EventApprovalHistory : BaseEntity
{
    public Guid EventId { get; set; }
    public virtual Event Event { get; set; }
    
    public EventStatus PreviousStatus { get; set; }
    public EventStatus NewStatus { get; set; }
    
    public string? Comment { get; set; } // Lý do duyệt/từ chối
    
    public Guid? ApprovedByUserId { get; set; } // Admin user ID
    public string? ApprovedByUserName { get; set; }
    
    public DateTime ActionDate { get; set; } = DateTime.UtcNow;
}

