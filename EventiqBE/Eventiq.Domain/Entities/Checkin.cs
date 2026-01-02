namespace Eventiq.Domain.Entities;

public class Checkin : BaseEntity
{
    public Guid TicketId { get; set; }
    public virtual Ticket Ticket { get; set; }
    
    public Guid EventItemId { get; set; }
    public virtual EventItem EventItem { get; set; }
    
    public Guid StaffId { get; set; }
    public virtual Staff Staff { get; set; }
    
    public DateTime CheckinTime { get; set; }
}
