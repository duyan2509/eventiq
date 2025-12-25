namespace Eventiq.Application.Dtos;

public class EventTaskDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public List<TaskOptionDto> Options { get; set; } = new();
    public List<AssignedStaffDto> AssignedStaffs { get; set; } = new();
}

public class AssignedStaffDto
{
    public Guid StaffId { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
}

public class CreateEventTaskDto
{
    public Guid EventId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public List<string> Options { get; set; } = new(); // List of option names (e.g., ["Gate A", "Gate B"])
}

public class UpdateEventTaskDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class AssignTaskToStaffDto
{
    public Guid TaskId { get; set; }
    public Guid OptionId { get; set; }
    public Guid StaffId { get; set; }
}

