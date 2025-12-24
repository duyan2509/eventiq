namespace Eventiq.Application.Dtos;

/// <summary>
/// Message  admin approve 
/// </summary>
public class EventProcessingMessage
{
    public Guid EventId { get; set; }
    public Guid AdminUserId { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}

