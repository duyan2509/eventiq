namespace Eventiq.Application.Dtos;

public class StaffInvitationDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public Guid InvitedUserId { get; set; }
    public string? InvitedUserName { get; set; }
    public string? InvitedUserEmail { get; set; }
    public DateTime InviteExpiredAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? RespondedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateStaffInvitationDto
{
    public Guid EventId { get; set; }
    public Guid OrganizationId { get; set; }
    public required string InvitedUserEmail { get; set; }
    public DateTime InviteExpiredAt { get; set; }
}

public class RespondToInvitationDto
{
    public Guid InvitationId { get; set; }
    public bool Accept { get; set; }
}

