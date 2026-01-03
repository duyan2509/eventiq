namespace Eventiq.Domain.Entities;

public enum TransferRequestStatus
{
    PENDING = 0,
    ACCEPTED = 1,
    REJECTED = 2,
    EXPIRED = 3
}

public class TransferRequest : BaseEntity
{
    public Guid TicketId { get; set; }
    public virtual Ticket Ticket { get; set; }
    public string SenderUserId { get; set; } = string.Empty;
    public string ToUserId { get; set; } = string.Empty;
    public TransferRequestStatus Status { get; set; } = TransferRequestStatus.PENDING;
    public DateTime ExpiresAt { get; set; }
}
