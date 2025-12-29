namespace Eventiq.Domain.Entities;

public class EventItem: BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime End { get; set; }
    public DateTime Start { get; set; }
    public Guid EventId { get; set; }
    public virtual Event Event { get; set; }
    public virtual Chart Chart { get; set; }
    public Guid ChartId { get; set; }
    public string? EventKey { get; set; } // Seats.io event key
    public int MaxPerUser { get; set; } = 0;
    public List<Ticket>  Tickets { get; set; } = new List<Ticket>();
}