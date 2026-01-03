namespace Eventiq.Application.Dtos;

public class TransferTicketRequestDto
{
    public string ToUserEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class TransferRequestDto
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
