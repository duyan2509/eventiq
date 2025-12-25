namespace Eventiq.Domain.Entities;

public class StaffInvitation : BaseEntity
{
    public Guid EventId { get; set; }
    public virtual Event Event { get; set; }
    
    public Guid OrganizationId { get; set; }
    public virtual Organization Organization { get; set; }
    
    public Guid InvitedUserId { get; set; }
    
    public DateTime EventStartTime { get; set; }
    public DateTime EventEndTime { get; set; }
    
    public DateTime InviteExpiredAt { get; set; }
    
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    
    public DateTime? RespondedAt { get; set; }
}

public enum InvitationStatus
{
    Pending,
    Accepted,
    Rejected,
    Expired
}

