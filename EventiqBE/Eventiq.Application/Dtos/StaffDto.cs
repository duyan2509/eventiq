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

