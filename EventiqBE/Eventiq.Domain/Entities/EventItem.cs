namespace Eventiq.Domain.Entities;

public class EventItem: BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime End { get; set; }
    public DateTime Start { get; set; }
    public Guid EventId { get; set; }
    public virtual Event Event { get; set; }
    public string? ChartKey { get; set; }
    public List<Ticket>  Tickets { get; set; } = new List<Ticket>();
    
}