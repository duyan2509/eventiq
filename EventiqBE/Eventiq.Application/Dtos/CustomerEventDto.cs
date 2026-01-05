namespace Eventiq.Application.Dtos;

public class CustomerEventListDto
{
    public List<CustomerEventDto> UpcomingEvents { get; set; } = new();
    public List<CustomerEventDto> PastEvents { get; set; } = new();
}

public class CustomerEventDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Banner { get; set; }
    public required DateTime Start { get; set; }
    public decimal? LowestPrice { get; set; } // Giá vé thấp nhất
    public required string OrganizationName { get; set; }
    public string? ProvinceName { get; set; }
}

public class CustomerEventDetailDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Banner { get; set; }
    public required DateTime Start { get; set; }
    public required EventAddressDto EventAddress { get; set; }
    public required string OrganizationName { get; set; }
    public List<CustomerEventItemDto> EventItems { get; set; } = new();
}

public class CustomerEventItemDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public decimal? LowestPrice { get; set; } // Giá vé thấp nhất trong EventItem
}

public class CustomerSeatMapDto
{
    public Guid EventItemId { get; set; }
    public required string EventItemName { get; set; }
    public Guid ChartId { get; set; }
    public required string ChartKey { get; set; }
    public string? EventKey { get; set; } // Seats.io event key (preferred over ChartKey)
    public required string ChartName { get; set; }
    public string? VenueDefinition { get; set; } // JSON string từ Seats.io
    public int MaxPerUser { get; set; } = 0; // Số lượng ghế tối đa mỗi người dùng có thể mua (0 = không giới hạn)
    public List<CustomerSeatDto> Seats { get; set; } = new();
    public List<CustomerTicketClassDto> TicketClasses { get; set; } = new(); // Danh sách loại vé với giá
}

public class CustomerSeatDto
{
    public Guid EventSeatId { get; set; }
    public required string SeatKey { get; set; }
    public string? Label { get; set; }
    public string? Section { get; set; }
    public string? Row { get; set; }
    public string? Number { get; set; }
    public string? CategoryKey { get; set; }
    public required string Status { get; set; } // "free", "paid", "hold", "reserved"
    public decimal? Price { get; set; } // Giá vé của ghế này (từ TicketClass)
}

public class CustomerTicketClassDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public string? Color { get; set; } // Màu của category trong Seats.io
}

