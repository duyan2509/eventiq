using System.ComponentModel.DataAnnotations;

namespace Eventiq.Domain.Entities;

public sealed class Organization: BaseEntity
{
    public string UserId { get; set; }
    [StringLength(maximumLength:30, MinimumLength = 2)]
    public required string Name { get; set; }
    public required string Avatar { get; set; }
    public List<Event> Events { get; set; } = new List<Event>();
}