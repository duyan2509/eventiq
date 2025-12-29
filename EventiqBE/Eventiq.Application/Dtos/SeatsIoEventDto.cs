using System.Text.Json.Serialization;

namespace Eventiq.Application.Dtos;

/// <summary>
/// DTO for Seats.io Event object retrieved from API
/// </summary>
public class SeatsIoEventDto
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("chartKey")]
    public string? ChartKey { get; set; }

    [JsonPropertyName("tableBookingConfig")]
    public TableBookingConfigDto? TableBookingConfig { get; set; }

    [JsonPropertyName("supportsBestAvailable")]
    public bool? SupportsBestAvailable { get; set; }

    [JsonPropertyName("forSaleConfig")]
    public ForSaleConfigDto? ForSaleConfig { get; set; }

    [JsonPropertyName("channels")]
    public List<ChannelDto>? Channels { get; set; }

    [JsonPropertyName("categories")]
    public List<CategoryDto>? Categories { get; set; }

    [JsonPropertyName("isSeason")]
    public bool? IsSeason { get; set; }

    [JsonPropertyName("isTopLevelSeason")]
    public bool? IsTopLevelSeason { get; set; }

    [JsonPropertyName("isPartialSeason")]
    public bool? IsPartialSeason { get; set; }

    [JsonPropertyName("isEventInSeason")]
    public bool? IsEventInSeason { get; set; }

    [JsonPropertyName("createdOn")]
    public DateTime? CreatedOn { get; set; }

    [JsonPropertyName("updatedOn")]
    public DateTime? UpdatedOn { get; set; }

    [JsonPropertyName("isInThePast")]
    public bool? IsInThePast { get; set; }
}

public class TableBookingConfigDto
{
    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    [JsonPropertyName("tables")]
    public Dictionary<string, string>? Tables { get; set; }
}

public class ForSaleConfigDto
{
    [JsonPropertyName("forSale")]
    public bool? ForSale { get; set; }

    [JsonPropertyName("objects")]
    public List<string>? Objects { get; set; }

    [JsonPropertyName("categories")]
    public List<string>? Categories { get; set; }
}

public class ChannelDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("objects")]
    public List<string>? Objects { get; set; }
}

public class CategoryDto
{
    [JsonPropertyName("key")]
    public object? Key { get; set; } // Can be string or number

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("accessible")]
    public bool? Accessible { get; set; }
}

