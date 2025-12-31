namespace Eventiq.Application.Dtos;

public class CheckoutDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid EventItemId { get; set; }
    public string Status { get; set; } = "INIT";
    public List<string> SeatIds { get; set; } = new();
    public string? HoldToken { get; set; }
    public DateTime? HoldTokenExpiresAt { get; set; }
    public string? EventKey { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCheckoutRequest
{
    public Guid EventItemId { get; set; }
    public List<string> SeatIds { get; set; } = new();
}

