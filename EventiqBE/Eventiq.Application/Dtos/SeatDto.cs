using Eventiq.Domain.Entities;

namespace Eventiq.Application.Dtos;

public class SeatInfoDto
{
    public string SeatKey { get; set; } = default!;
    public string? Label { get; set; }
    public string? Section { get; set; }
    public string? Row { get; set; }
    public string? Number { get; set; }
    public string? CategoryKey { get; set; }
    public string? CategoryLabel { get; set; }
    public object? ExtraData { get; set; }
}

public class SyncSeatsRequestDto
{
    public IEnumerable<SeatInfoDto> Seats { get; set; } = new List<SeatInfoDto>();
    public string? VenueDefinition { get; set; } // JSON string chứa cấu hình seat map từ Seats.io
    public string? ChartKey { get; set; } // Seats.io chart key từ designer
}

public class SyncSeatsResponseDto
{
    public int TotalSeats { get; set; }
    public int UpdatedSeats { get; set; }
    public int NewSeats { get; set; }
    public bool Success { get; set; }
}

public class SeatMapViewDto
{
    public Guid ChartId { get; set; }
    public string ChartKey { get; set; } = default!;
    public string ChartName { get; set; } = default!;
    public string? VenueDefinition { get; set; } // JSON string chứa cấu hình seat map từ DB
    public IEnumerable<SeatWithStatusDto> Seats { get; set; } = new List<SeatWithStatusDto>();
    public bool IsReadOnly { get; set; } // true cho user view, false cho org config
}

public class SeatWithStatusDto
{
    public Guid EventSeatId { get; set; }
    public string SeatKey { get; set; } = default!;
    public string? Label { get; set; }
    public string? Section { get; set; }
    public string? Row { get; set; }
    public string? Number { get; set; }
    public string? CategoryKey { get; set; }
    public string Status { get; set; } = default!; // "free", "paid", "hold" (hold sẽ từ Redis)
    public object? ExtraData { get; set; }
}

