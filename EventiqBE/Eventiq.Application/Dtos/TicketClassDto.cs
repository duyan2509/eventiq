namespace Eventiq.Application.Dtos;

public class TicketClassDto
{
    public decimal Price { get; set; }
    public string Name { get; set; }
    public int TotalQuantity { get; set; }  = 0;
    public int SoldQuantity { get; set; } = 0;
    public int MaxPerUser { get; set; }
    public DateTime SaleStart { get; set; }
    public DateTime SaleEnd { get; set; }
}
public class CreateTicketClassDto
{
    public decimal Price { get; set; }
    public string Name { get; set; }
    public int MaxPerUser { get; set; } = 1;
    public DateTime SaleStart { get; set; }
    public DateTime SaleEnd { get; set; }

    public bool Valid()
    {
        return SaleStart < SaleEnd;
    }
}

public class UpdateTicketClassInfoDto
{
    public decimal? Price { get; set; }
    public string? Name { get; set; }
    public int? MaxPerUser { get; set; }
    public DateTime? SaleStart { get; set; }
    public DateTime? SaleEnd { get; set; }
}