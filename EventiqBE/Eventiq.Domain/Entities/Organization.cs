namespace Eventiq.Domain.Entities;

public sealed class Organization: BaseEntity
{
    public string UserId { get; set; }
    public required string Name { get; set; }
    public List<Event> Events { get; set; } = new List<Event>();
}