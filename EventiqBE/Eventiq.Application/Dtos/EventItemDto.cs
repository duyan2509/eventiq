namespace Eventiq.Application.Dtos;

public class EventItemDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime End { get; set; }
    public DateTime Start { get; set; }
    public string? ChartKey { get; set; }
}
public class CreateEventItemDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime End { get; set; }
    public DateTime Start { get; set; }

    public bool CheckValidTime()
    {
        return Start < End;
    }
}

public class UpdateEventItemDto
{
    public required string? Name { get; set; }
    public string? Description { get; set; }
    public DateTime? End { get; set; }
    public DateTime? Start { get; set; }
}

public class EventCharKey
{
    public required string ChartKey { get; set; }
}