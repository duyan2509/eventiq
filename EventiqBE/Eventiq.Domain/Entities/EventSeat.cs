using System.ComponentModel.DataAnnotations;

namespace Eventiq.Domain.Entities;

public class EventSeat : BaseEntity
{
    public Guid ChartId { get; set; }
    public virtual Chart Chart { get; set; }

    public string SeatKey { get; set; } = default!;

    public string? Label { get; set; } 
    public string? Section { get; set; }
    public string? Row { get; set; }
    public string? Number { get; set; }

    public string? CategoryKey { get; set; }

    public string? ExtraData { get; set; }

    public virtual ICollection<EventSeatState> SeatStates { get; set; } =
        new HashSet<EventSeatState>();
}
