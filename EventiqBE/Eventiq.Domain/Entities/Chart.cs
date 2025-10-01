namespace Eventiq.Domain.Entities;

public class Chart:BaseEntity
{
    public string Name { get; set; }
    public string Key {get; set;}
    public virtual Event Event { get; set; }
    public Guid EventId { get; set; }
    
    public virtual IEnumerable<EventItem> EventItems { get; set; }
}