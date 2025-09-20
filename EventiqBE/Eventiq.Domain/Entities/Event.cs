namespace Eventiq.Domain.Entities;

public class Event:BaseEntity
{
    public required string Banner { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required DateTime Start { get; set; } = DateTime.MinValue;
    public virtual ICollection<EventItem> EventItem { get; set; }  = new  List<EventItem>();
    public virtual ICollection<TicketClass> TicketClasses { get; set; } = new List<TicketClass>();
    public virtual required Organization Organization { get; set; } 
    public Guid OrganizationId { get; set; }
    public int BankCode { get; set; }
    public string AccountNumber { get; set; } = "";
    public string AccountName { get; set; } = "";
    public virtual EventAddress EventAddress { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Draft;

}

public enum EventStatus
{
    Draft,
    Published,
    Pending
}