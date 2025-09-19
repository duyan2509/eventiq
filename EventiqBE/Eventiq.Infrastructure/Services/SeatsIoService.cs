using Eventiq.Application.Interfaces.Services;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Eventiq.Infrastructure.Services;

public class SeatsIoService : ISeatService
{
    private readonly HttpClient _httpClient;

    public SeatsIoService(IConfiguration config)
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://api.seatsio.net/");
        var secretKey = config["SeatsIo:SecretKey"];
        var byteArray = Encoding.ASCII.GetBytes(secretKey + ":");
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
    }

    public async Task<JsonDocument> BookSeats(string eventKey, List<string> seats)
    {
        return await Post($"events/{eventKey}/actions/book", new { seats });
    }

    public async Task<JsonDocument> HoldSeats(string eventKey, List<string> seats)
    {
        var holdToken = await CreateHoldToken();
        return await Post($"events/{eventKey}/actions/hold", new { seats, holdToken });
    }

    public async Task<JsonDocument> ReleaseSeats(string eventKey, List<string> seats)
    {
        return await Post($"events/{eventKey}/actions/release", new { seats });
    }

    public async Task<JsonDocument> CreateEvent(string chartKey)
    {
        return await Post("events", new { chartKey });
    }

    public async Task<string> CreateHoldToken()
    {
        var response = await _httpClient.PostAsync("hold-tokens", null);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("holdToken").GetString()!;
    }

    private async Task<JsonDocument> Post(string url, object body)
    {
        var json = JsonSerializer.Serialize(body);
        var response = await _httpClient.PostAsync(url,
            new StringContent(json, Encoding.UTF8, "application/json"));

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content);
    }
    
}
