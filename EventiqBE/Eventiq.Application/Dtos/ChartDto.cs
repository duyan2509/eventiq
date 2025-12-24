namespace Eventiq.Application.Dtos;

public class ChartDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Key { get; set; }
    public string? VenueDefinition { get; set; } // JSON string chứa cấu hình seat map
    public Guid EventId { get; set; }
}

public class UpdateChartDto
{
    public required string Name { get; set; }
}

public class CreateChartDto
{
    public required string Name { get; set; }
}
