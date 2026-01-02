namespace Eventiq.Api.Request;

public class CreateStaffInvitationRequest
{
    public Guid EventId { get; set; }
    public Guid OrganizationId { get; set; }
    public required string InvitedUserEmail { get; set; }
    public DateTime InviteExpiredAt { get; set; }
}

public class RespondToInvitationRequest
{
    public bool Accept { get; set; }
}

public class CreateEventTaskRequest
{
    public Guid EventId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public List<string> Options { get; set; } = new(); // List of option names (e.g., ["Gate A", "Gate B"])
}

public class UpdateEventTaskRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class AssignTaskToStaffRequest
{
    public Guid TaskId { get; set; }
    public Guid OptionId { get; set; }
    public Guid StaffId { get; set; }
}

public class CreateTaskOptionRequest
{
    public Guid TaskId { get; set; }
    public required string OptionName { get; set; }
}

public class UpdateTaskOptionRequest
{
    public required string OptionName { get; set; }
}

