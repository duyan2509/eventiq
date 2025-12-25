namespace Eventiq.Application.Dtos;

public class TaskOptionDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public string OptionName { get; set; } = string.Empty;
    public List<AssignedStaffDto> AssignedStaffs { get; set; } = new();
}

public class CreateTaskOptionDto
{
    public Guid TaskId { get; set; }
    public required string OptionName { get; set; }
}

