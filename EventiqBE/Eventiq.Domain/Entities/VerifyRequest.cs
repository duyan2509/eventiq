namespace Eventiq.Domain.Entities;

public enum VerifyRequestStatus
{
    OWNER_VERIFIED = 0,
    SUCCESS = 1,
    EXPIRED = 2
}

public class VerifyRequest : BaseEntity
{
    public Guid TicketId { get; set; }
    public virtual Ticket Ticket { get; set; }
    public string OtpHash { get; set; }
    public VerifyRequestStatus Status { get; set; } = VerifyRequestStatus.OWNER_VERIFIED;
    public DateTime ExpiresAt { get; set; }
    public Guid? VerifiedBy { get; set; }
}
