namespace Eventiq.Domain.Entities;

public class TicketClass:BaseEntity
{
    public Guid EventId { get; set; }
    public virtual Event Event { get; set; }
    public virtual ICollection<Ticket> Tickets { get; set; } = new HashSet<Ticket>();
    public decimal Price { get; set; }
    public string Name { get; set; }
}