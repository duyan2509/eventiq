namespace Eventiq.Domain.Entities;

public class Event:BaseEntity
{
    public required string Banner { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required DateTime Start { get; set; }
    public required DateTime End { get; set; }
    public virtual ICollection<EventItem> EventItem { get; set; }  = new  List<EventItem>();
    public virtual ICollection<TicketClass> TicketClasses { get; set; } = new List<TicketClass>();
    public virtual required Organization Organization { get; set; } 
    public Guid OrganizationId { get; set; }
    public string? PaymentQR {get; set;}
    public Guid EventAddressId { get; set; }
    public virtual EventAddress EventAddress { get; set; }
    
}