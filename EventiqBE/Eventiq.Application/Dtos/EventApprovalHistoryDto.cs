using Eventiq.Domain.Entities;

namespace Eventiq.Application.Dtos;

public class EventApprovalHistoryDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string PreviousStatus { get; set; } = default!; 
    public string NewStatus { get; set; } = default!;  
    public string? Comment { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? ApprovedByUserName { get; set; }
    public DateTime ActionDate { get; set; }
}

