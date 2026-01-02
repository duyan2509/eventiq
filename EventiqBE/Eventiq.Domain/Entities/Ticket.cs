namespace Eventiq.Domain.Entities;

public enum TicketStatus
{
    UNUSED = 0,
    USED = 1
}

public class Ticket:BaseEntity
{
    public Guid TicketClassId { get; set; }
    public Guid EventItemId { get; set; }
    public string UserId { get; set; }
    public string TicketCode { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.UNUSED;
    public virtual EventItem EventItem { get; set; }
    public virtual TicketClass TicketClass { get; set; }
    
}