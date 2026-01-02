namespace Eventiq.Application.Dtos;

public class StaffDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<TaskAssignmentDto> AssignedTasks { get; set; } = new();
}

public class TaskAssignmentDto
{
    public Guid TaskId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public Guid OptionId { get; set; }
    public string OptionName { get; set; } = string.Empty;
}

public class StaffListDto
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public List<StaffDto> Staffs { get; set; } = new();
}

public class StaffCalendarEventDto
{
    public Guid StaffId { get; set; }
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime Date { get; set; }
}

public class StaffCalendarDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public List<StaffCalendarEventDto> Events { get; set; } = new();
}

public class CurrentShiftDto
{
    public Guid StaffId { get; set; }
    public Guid EventId { get; set; }
    public Guid EventItemId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<AssignedTaskDto> AssignedTasks { get; set; } = new();
}

public class AssignedTaskDto
{
    public Guid TaskId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public Guid OptionId { get; set; }
    public string OptionName { get; set; } = string.Empty;
}

public class VerifyTicketRequest
{
    public string TicketId { get; set; } = string.Empty;
}

public class VerifyTicketResponse
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public TicketInfoDto? TicketInfo { get; set; }
}

public class TicketInfoDto
{
    public Guid TicketId { get; set; }
    public Guid EventItemId { get; set; }
    public string EventItemName { get; set; } = string.Empty;
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string TicketClassName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
}
