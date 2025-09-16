namespace Eventiq.Domain.Entities;

public class Ticket:BaseEntity
{
    public Guid TicketClassId { get; set; }
    public Guid EventItemId { get; set; }
    public string UserId { get; set; }
    public virtual EventItem EventItem { get; set; }
    public virtual TicketClass TicketClass { get; set; }
    
}