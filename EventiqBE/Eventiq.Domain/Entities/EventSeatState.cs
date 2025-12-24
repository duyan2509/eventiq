using System.ComponentModel.DataAnnotations;

namespace Eventiq.Domain.Entities;

public enum SeatStatus
{
    Free = 0,
    Paid = 1,
}

public class EventSeatState : BaseEntity
{
    public Guid EventSeatId { get; set; }
    public virtual EventSeat EventSeat { get; set; }

    public Guid EventItemId { get; set; }
    public virtual EventItem EventItem { get; set; }
    public SeatStatus Status { get; set; } = SeatStatus.Free;

    public Guid? TicketId { get; set; }
    public virtual Ticket? Ticket { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
