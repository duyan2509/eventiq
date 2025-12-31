using System.ComponentModel.DataAnnotations;

namespace Eventiq.Domain.Entities;

public class EventSeat : BaseEntity
{
    public Guid ChartId { get; set; }
    public virtual Chart Chart { get; set; }

    public required string Label { get; set; } // Unique identifier from Seats.io (e.g., "b-1", "b-2") 
    public string? Section { get; set; }
    public string? Row { get; set; }
    public string? Number { get; set; }

    public string? CategoryKey { get; set; }

    public string? ExtraData { get; set; }

    public virtual ICollection<EventSeatState> SeatStates { get; set; } =
        new HashSet<EventSeatState>();
}
