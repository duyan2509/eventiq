namespace Eventiq.Application.Dtos;

public class TicketClassDto
{
    public Guid Id { get; set; }
    public decimal Price { get; set; }
    public string Name { get; set; }
    public int TotalQuantity { get; set; }  = 0;
    public int SoldQuantity { get; set; } = 0;
}
public class CreateTicketClassDto
{
    public decimal Price { get; set; }
    public string Name { get; set; }
}

public class UpdateTicketClassInfoDto
{
    public decimal? Price { get; set; }
    public string? Name { get; set; }
}