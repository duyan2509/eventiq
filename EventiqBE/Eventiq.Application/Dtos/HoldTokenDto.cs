using System.Text.Json.Serialization;

namespace Eventiq.Application.Dtos;

public class HoldTokenDto
{
    [JsonPropertyName("holdToken")]
    public string HoldToken { get; set; } = string.Empty;
    
    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; set; }
    
    [JsonPropertyName("expiresInSeconds")]
    public int ExpiresInSeconds { get; set; }
}

