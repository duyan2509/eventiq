using Eventiq.Application.Interfaces.Services;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Eventiq.Application.Dtos;
using Eventiq.Domain.Entities;
using Microsoft.Extensions.Configuration;
using SeatsioDotNet;
using SeatsioDotNet.Charts;
using System.Net.Http;

namespace Eventiq.Infrastructure.Services;

public class SeatsIoService : ISeatService
{
    private readonly SeatsioClient client;
    private readonly string? _secretKey;
    private readonly HttpClient _httpClient;

    public SeatsIoService(string? secretKey)
    {
        client = new SeatsioClient(Region.OC(), secretKey);
        _secretKey = secretKey;
        _httpClient = new HttpClient();
    }

    public async Task<string> CreateChartAsync(IEnumerable<TicketClass> ticketClasses)
    {
        var chart = await client.Charts.CreateAsync();

        var tasks = ticketClasses.Select(ticketClass =>
            client.Charts.AddCategoryAsync(
                chart.Key,
                new Category()
                {
                    Key = chart.Key,
                    Color = GenerateRandomColor(),
                    Label = ticketClass.Name
                }
            )
        );

        await Task.WhenAll(tasks);

        return chart.Key;
    }

    public async Task<IEnumerable<SeatInfoDto>> GetSeatsFromChartAsync(string chartKey)
    {
        try
        {
            // Retrieve chart to get objects
            var chart = await client.Charts.RetrieveAsync(chartKey);
            var seats = new List<SeatInfoDto>();

            // Seats.io stores objects in the chart's venue definition
            // We need to parse the chart data to extract seat information
            // For now, we'll use a workaround: create a temporary event to list objects
            // Or use the chart's object info directly if available
            
            // Note: Seats.io .NET SDK may not have direct method to list all objects from chart
            // This is a simplified implementation - you may need to use REST API directly
            // or use Events API to retrieve objects after creating an event from the chart
            
            // For now, return empty list and note that seats should be synced from Seats.io designer
            // The actual seat data will come from the frontend when user clicks Save in the designer
            
            return seats;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to retrieve seats from Seats.io chart: {ex.Message}. Note: Seats should be synced from the Seats.io designer interface.", ex);
        }
    }

    public Task<Dictionary<string, string>> GetSeatStatusForEventItemAsync(string chartKey, Guid eventItemId, IEnumerable<string> seatKeys)
    {
        // This method would typically retrieve seat statuses from Seats.io events
        // For now, return empty dictionary as statuses are managed in our DB via EventSeatState
        // This can be enhanced later if needed for real-time sync
        return Task.FromResult(new Dictionary<string, string>());
    }

    public async Task<string> GetVenueDefinitionFromChartAsync(string chartKey)
    {
        try
        {
            try
            {
                var apiUrl = $"https://api.seats.io/v2/charts/{chartKey}";
                
                var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", 
                    Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_secretKey}:")));
                
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonContent);
                    var root = doc.RootElement;
                    
                    if (root.TryGetProperty("venue", out var venueElement))
                    {
                        return venueElement.GetRawText();
                    }
                    

                    return jsonContent;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new Exception($"Chart {chartKey} not found in Seats.io. Chart may not exist or may have been deleted.");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Seats.io API returned status {response.StatusCode}: {errorContent}");
                }
            }
            catch (HttpRequestException httpEx)
            {
                try
                {
                    var chart = await client.Charts.RetrieveAsync(chartKey);
                    
                    if (chart == null)
                    {
                        throw new Exception($"Chart {chartKey} not found in Seats.io");
                    }
                    
                    // Serialize chart object
                    var chartJson = System.Text.Json.JsonSerializer.Serialize(chart, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = false,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    });
                    
                    // Parse để lấy venue nếu có
                    using var doc = JsonDocument.Parse(chartJson);
                    var root = doc.RootElement;
                    
                    if (root.TryGetProperty("venue", out var venueElement))
                    {
                        return venueElement.GetRawText();
                    }
                    
                    return chartJson;
                }
                catch (Exception sdkEx)
                {
                    throw new Exception($"Failed to retrieve venue definition from Seats.io chart {chartKey}. REST API error: {httpEx.Message}. SDK error: {sdkEx.Message}", httpEx);
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to retrieve venue definition from Seats.io chart {chartKey}: {ex.Message}", ex);
        }
    }

    private string GenerateRandomColor()
    {
        var random = new Random();
        return $"#{random.Next(0x1000000):X6}"; 
    }
}
