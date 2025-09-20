using Eventiq.Application.Dtos;

namespace Eventiq.Api.Request;

public class CreateEventRequest
{
    public required IFormFile Banner { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Guid OrganizationId { get; set; }
    public required EventAddressDto EventAddress { get; set; }
}

public class UpdateEventRequest
{
    public IFormFile Banner { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Guid? OrganizationId { get; set; }
}