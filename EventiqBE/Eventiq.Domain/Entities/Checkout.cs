namespace Eventiq.Domain.Entities;

public class Checkout : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid EventItemId { get; set; }
    public virtual EventItem? EventItem { get; set; }
    
    public string Status { get; set; } = "INIT"; // INIT, SUCCESS, CANCELED
    
    public string? HoldToken { get; set; } // Seats.io hold token
    public DateTime? HoldTokenExpiresAt { get; set; }
    
    public string? EventKey { get; set; } // Seats.io event key
    
    // Store seat IDs as JSON string
    public string SeatIdsJson { get; set; } = "[]";
    
    // Payment relationship
    public virtual Payment? Payment { get; set; }
    
    public List<string> GetSeatIds()
    {
        return System.Text.Json.JsonSerializer.Deserialize<List<string>>(SeatIdsJson) ?? new List<string>();
    }
    
    public void SetSeatIds(List<string> seatIds)
    {
        SeatIdsJson = System.Text.Json.JsonSerializer.Serialize(seatIds);
    }
}

