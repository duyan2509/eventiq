namespace Eventiq.Domain.Entities;

public class Chart:BaseEntity
{
    public string Name { get; set; }
    public string Key {get; set;}
    public string? VenueDefinition { get; set; } // JSON string chứa cấu hình seat map từ Seats.io
    public virtual Event Event { get; set; }
    public Guid EventId { get; set; }
    public virtual IEnumerable<EventSeat> EventSeats { get; set; }
    public virtual IEnumerable<EventItem> EventItems { get; set; }
}