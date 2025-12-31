using Eventiq.Application.Interfaces.Services;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Eventiq.Application.Dtos;
using Eventiq.Domain.Entities;
using Microsoft.Extensions.Configuration;
using SeatsioDotNet;
using SeatsioDotNet.Charts;
using SeatsioDotNet.Events;
using System.Net.Http;
using System.Reflection;

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
        {
            var color = GenerateRandomColor();
            return new { TicketClass = ticketClass, Color = color, Task = client.Charts.AddCategoryAsync(
                chart.Key,
                new Category()
                {
                    Key = ticketClass.Name, 
                    Color = color,
                    Label = ticketClass.Name,
                }
            )};
        }).ToList();

        await Task.WhenAll(tasks.Select(t => t.Task));

        foreach (var task in tasks)
        {
            task.TicketClass.Color = task.Color;
        }

        return chart.Key;
    }

    public async Task<IEnumerable<SeatInfoDto>> GetSeatsFromChartAsync(string chartKey)
    {
        try
        {
            // Retrieve chart to get objects
            var chart = await client.Charts.RetrieveAsync(chartKey);
            var seats = new List<SeatInfoDto>();

            
            return seats;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to retrieve seats from Seats.io chart: {ex.Message}. Note: Seats should be synced from the Seats.io designer interface.", ex);
        }
    }

    public Task<Dictionary<string, string>> GetSeatStatusForEventItemAsync(string chartKey, Guid eventItemId, IEnumerable<string> seatKeys)
    {

        return Task.FromResult(new Dictionary<string, string>());
    }

    public async Task<string> GetVenueDefinitionFromChartAsync(string chartKey)
    {
        try
        {
            // Try SDK first - RetrievePublishedVersionAsync (most reliable)
            try
            {
                var drawing = await client.Charts.RetrievePublishedVersionAsync(chartKey);
                
                // Serialize drawing object to JSON
                var drawingJson = System.Text.Json.JsonSerializer.Serialize(drawing, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                
                // Try to extract venue from serialized drawing
                using var doc = JsonDocument.Parse(drawingJson);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("venue", out var venueElement))
                {
                    return venueElement.GetRawText();
                }
                
                // If no venue in published version, try RetrieveAsync (draft version)
                var chart = await client.Charts.RetrieveAsync(chartKey);
                if (chart != null)
                {
                    var chartJson = System.Text.Json.JsonSerializer.Serialize(chart, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = false,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    });
                    
                    using var chartDoc = JsonDocument.Parse(chartJson);
                    var chartRoot = chartDoc.RootElement;
                    
                    if (chartRoot.TryGetProperty("venue", out var chartVenueElement))
                    {
                        return chartVenueElement.GetRawText();
                    }
                    
                    return chartJson;
                }
            }
            catch (Exception sdkEx)
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
                        throw new Exception($"Chart {chartKey} not found in Seats.io via REST API. SDK error: {sdkEx.Message}");
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Seats.io REST API returned status {response.StatusCode}: {errorContent}. SDK error: {sdkEx.Message}");
                    }
                }
                catch (Exception restEx)
                {
                    throw new Exception($"Failed to retrieve venue definition from Seats.io chart {chartKey}. SDK error: {sdkEx.Message}. REST API error: {restEx.Message}", sdkEx);
                }
            }
            
            throw new Exception($"Chart {chartKey} retrieved but no venue definition found");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to retrieve venue definition from Seats.io chart {chartKey}: {ex.Message}", ex);
        }
    }

    public async Task<string> CreateEventFromChartAsync(string chartKey, string? eventKey = null)
    {
        try
        {
            
            var evnt = await client.Events.CreateAsync(chartKey);
            
            return evnt.Key;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to create event from chart {chartKey}: {ex.Message}", ex);
        }
    }

    public async Task<SeatsIoEventDto> RetrieveEventAsync(string eventKey)
    {
        try
        {
            // Use Seats.io .NET SDK to retrieve event
            var evnt = await client.Events.RetrieveAsync(eventKey);
            
            var dto = new SeatsIoEventDto
            {
                Id = evnt.Id,
                Key = evnt.Key ?? eventKey,
                ChartKey = evnt.ChartKey,
                SupportsBestAvailable = evnt.SupportsBestAvailable,
                IsSeason = evnt.IsSeason,
                IsTopLevelSeason = evnt.IsTopLevelSeason,
                IsPartialSeason = evnt.IsPartialSeason,
                IsEventInSeason = evnt.IsEventInSeason,
                CreatedOn = evnt.CreatedOn?.DateTime,
                UpdatedOn = evnt.UpdatedOn?.DateTime,
                IsInThePast = evnt.IsInThePast
            };

            // Map TableBookingConfig if available
            if (evnt.TableBookingConfig != null)
            {
                dto.TableBookingConfig = new TableBookingConfigDto
                {
                    Mode = evnt.TableBookingConfig.Mode,
                    Tables = evnt.TableBookingConfig.Tables
                };
            }

            // Map ForSaleConfig if available
            if (evnt.ForSaleConfig != null)
            {
                dto.ForSaleConfig = new ForSaleConfigDto
                {
                    ForSale = evnt.ForSaleConfig.ForSale,
                    Objects = evnt.ForSaleConfig.Objects?.ToList(),
                    Categories = evnt.ForSaleConfig.Categories?.ToList()
                };
            }

            // Map Channels if available
            if (evnt.Channels != null && evnt.Channels.Any())
            {
                dto.Channels = evnt.Channels.Select(ch => new ChannelDto
                {
                    Name = ch.Name,
                    Key = ch.Key,
                    Color = ch.Color,
                    Objects = ch.Objects?.ToList()
                }).ToList();
            }

            // Map Categories if available
            if (evnt.Categories != null && evnt.Categories.Any())
            {
                dto.Categories = evnt.Categories.Select(cat => new CategoryDto
                {
                    Key = cat.Key,
                    Label = cat.Label,
                    Color = cat.Color,
                    Accessible = cat.Accessible
                }).ToList();
            }

            return dto;
        }
        catch (Exception ex)
        {
            // If SDK fails, try REST API as fallback
            try
            {
                var apiUrl = $"https://api.seats.io/v2/events/{eventKey}";
                
                var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", 
                    Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_secretKey}:")));
                
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var dto = JsonSerializer.Deserialize<SeatsIoEventDto>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (dto == null)
                    {
                        throw new Exception("Failed to deserialize event response from Seats.io API");
                    }
                    
                    return dto;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new Exception($"Event {eventKey} not found in Seats.io");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Seats.io API returned status {response.StatusCode}: {errorContent}");
                }
            }
            catch (Exception restEx)
            {
                throw new Exception($"Failed to retrieve event {eventKey} from Seats.io. SDK error: {ex.Message}. REST API error: {restEx.Message}", ex);
            }
        }
    }

    public async Task<IEnumerable<SeatInfoDto>> GetAllObjectsFromEventAsync(string eventKey, int maxRetries = 3, int delayMs = 1000)
    {
        try
        {
            // Use Seats.io Event Reports API to get all objects from event
            var apiUrl = $"https://api.seats.io/v2/events/{eventKey}/reports/byLabel";
            
            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", 
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_secretKey}:")));
            
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                var seats = new List<SeatInfoDto>();
                
                // Parse JSON response from Event Reports API
                using var doc = JsonDocument.Parse(jsonContent);
                var root = doc.RootElement;
                
                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var obj in root.EnumerateArray())
                    {
                        var seat = new SeatInfoDto
                        {
                            SeatKey = obj.TryGetProperty("objectId", out var objectId) 
                                ? objectId.GetString() ?? "" 
                                : obj.TryGetProperty("id", out var id) 
                                    ? id.GetString() ?? "" 
                                    : "",
                            Label = obj.TryGetProperty("label", out var label) 
                                ? label.GetString() 
                                : null,
                            CategoryKey = obj.TryGetProperty("categoryKey", out var categoryKey) 
                                ? categoryKey.GetString() 
                                : null,
                            CategoryLabel = obj.TryGetProperty("categoryLabel", out var categoryLabel) 
                                ? categoryLabel.GetString() 
                                : null,
                        };
                        
                        // Extract section, row, number from labels if available
                        if (obj.TryGetProperty("labels", out var labels))
                        {
                            if (labels.TryGetProperty("section", out var section))
                                seat.Section = section.GetString();
                            if (labels.TryGetProperty("row", out var row))
                                seat.Row = row.GetString();
                            if (labels.TryGetProperty("seat", out var seatNum))
                                seat.Number = seatNum.GetString();
                        }
                        
                        if (obj.TryGetProperty("extraData", out var extraData))
                        {
                            seat.ExtraData = JsonSerializer.Deserialize<object>(extraData.GetRawText());
                        }
                        
                        if (!string.IsNullOrEmpty(seat.Label))
                        {
                            seats.Add(seat);
                        }
                        else if (!string.IsNullOrEmpty(seat.SeatKey))
                        {
                            seat.Label = seat.SeatKey;
                            seats.Add(seat);
                        }
                    }
                }
                
                return seats;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new Exception($"Event {eventKey} not found in Seats.io");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Seats.io API returned status {response.StatusCode}: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to retrieve objects from Seats.io event {eventKey}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get event report detail from Seats.io
    /// Returns detailed information about all objects (seats) in the event including status, category, etc.
    /// Uses SDK: client.EventReports.ByLabelAsync()
    /// </summary>
    public async Task<object> GetEventReportDetailAsync(string eventKey)
    {
        try
        {
            var report = await client.EventReports.ByLabelAsync(eventKey);
            
            return ProcessReportResult(report, eventKey, isEvent: true);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to retrieve event report detail from Seats.io event {eventKey}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get detailed chart report from Seats.io
    /// Returns detailed information about all objects (seats) in the chart including category, type, etc.
    /// Uses SDK: client.ChartReports.ByLabelAsync()
    /// </summary>
    public async Task<object> GetChartReportDetailAsync(string chartKey)
    {
        try
        {
            // Use Seats.io SDK to get chart report
            // Documentation: https://docs.seats.io/docs/api/chart-reports/detail/
            var report = await client.ChartReports.ByLabelAsync(chartKey);
            
            // Convert SDK result to our format
            return ProcessReportResult(report, chartKey, isEvent: false);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to retrieve chart report detail from Seats.io chart {chartKey}: {ex.Message}", ex);
        }
    }

    public async Task<object> RetrievePublishedChartVersionAsync(string chartKey)
    {
        try
        {
            var drawing = await client.Charts.RetrievePublishedVersionAsync(chartKey);
            
            var result = new Dictionary<string, object>
            {
                ["VenueType"] = drawing.VenueType ?? "Unknown"
            };
            
            // Add Categories if available
            if (drawing.Categories != null)
            {
                result["Categories"] = drawing.Categories.Select(c => new
                {
                    Key = c.Key,
                    Label = c.Label,
                    Color = c.Color,
                    Accessible = c.Accessible
                }).ToList();
                result["CategoriesCount"] = drawing.Categories.Count();
            }
            else
            {
                result["Categories"] = new List<object>();
                result["CategoriesCount"] = 0;
            }
            
            var properties = drawing.GetType().GetProperties();
            var availableProperties = new Dictionary<string, object>();
            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(drawing);
                    if (value != null)
                    {
                        availableProperties[prop.Name] = value;
                    }
                }
                catch
                {
                    // Skip properties that can't be read
                }
            }
            result["AvailableProperties"] = availableProperties.Keys.ToList();
            
            // Also try to get venue definition which contains objects
            try
            {
                var venueDefinition = await GetVenueDefinitionFromChartAsync(chartKey);
                if (!string.IsNullOrEmpty(venueDefinition))
                {
                    using var doc = JsonDocument.Parse(venueDefinition);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("objects", out var objectsElement))
                    {
                        var objectsList = new List<object>();
                        foreach (var obj in objectsElement.EnumerateArray())
                        {
                            var objDict = new Dictionary<string, object>();
                            foreach (var prop in obj.EnumerateObject())
                            {
                                objDict[prop.Name] = prop.Value.ToString();
                            }
                            objectsList.Add(objDict);
                        }
                        result["Objects"] = objectsList;
                        result["ObjectsCount"] = objectsList.Count;
                        result["HasObjects"] = objectsList.Count > 0;
                    }
                }
            }
            catch
            {
                result["Objects"] = new List<object>();
                result["ObjectsCount"] = 0;
                result["HasObjects"] = false;
            }
            
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to retrieve published chart version for chart {chartKey}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Helper method to process report result from SDK
    /// SDK returns Dictionary<string, List<object>> where key is label and value is list of objects
    /// </summary>
    private object ProcessReportResult(object result, string key, bool isEvent)
    {
        var report = new Dictionary<string, object>();
        
        if (isEvent)
        {
            report["eventKey"] = key;
        }
        else
        {
            report["chartKey"] = key;
        }
        
        // SDK returns Dictionary<string, List<object>> for byLabel reports
        // Convert to flat list of all objects
        var allObjects = new List<Dictionary<string, object>>();
        
        if (result is System.Collections.IDictionary dict)
        {
            foreach (System.Collections.DictionaryEntry entry in dict)
            {
                if (entry.Value is System.Collections.IEnumerable list)
                {
                    foreach (var obj in list)
                    {
                        var objDict = ConvertObjectToDictionary(obj);
                        if (objDict != null)
                        {
                            allObjects.Add(objDict);
                        }
                    }
                }
            }
        }
        else
        {
            // Try to serialize and parse as JSON if not dictionary
            var json = JsonSerializer.Serialize(result);
            return ProcessReportFromJson(json, key, isEvent);
        }
        
        return BuildReportDictionary(allObjects, key, isEvent);
    }

    /// <summary>
    /// Helper method to convert SDK object to dictionary
    /// </summary>
    private Dictionary<string, object>? ConvertObjectToDictionary(object obj)
    {
        if (obj == null) return null;
        
        var dict = new Dictionary<string, object>();
        
        // Try to serialize and parse as JSON to get all properties
        try
        {
            var json = JsonSerializer.Serialize(obj);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    dict[prop.Name] = prop.Value.GetString() ?? "";
                }
                else if (prop.Value.ValueKind == JsonValueKind.Number)
                {
                    dict[prop.Name] = prop.Value.GetDecimal();
                }
                else if (prop.Value.ValueKind == JsonValueKind.True || prop.Value.ValueKind == JsonValueKind.False)
                {
                    dict[prop.Name] = prop.Value.GetBoolean();
                }
                else if (prop.Value.ValueKind == JsonValueKind.Object || prop.Value.ValueKind == JsonValueKind.Array)
                {
                    dict[prop.Name] = JsonSerializer.Deserialize<object>(prop.Value.GetRawText());
                }
                else
                {
                    dict[prop.Name] = prop.Value.GetRawText();
                }
            }
        }
        catch
        {
            // If serialization fails, try reflection
            var type = obj.GetType();
            foreach (var prop in type.GetProperties())
            {
                try
                {
                    var value = prop.GetValue(obj);
                    if (value != null)
                    {
                        dict[prop.Name] = value;
                    }
                }
                catch
                {
                    // Skip properties that can't be read
                }
            }
        }
        
        return dict;
    }

    /// <summary>
    /// Helper method to process report from JSON string
    /// </summary>
    private object ProcessReportFromJson(string jsonContent, string key, bool isEvent)
    {
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;
        
        if (root.ValueKind == JsonValueKind.Array)
        {
            var objects = new List<Dictionary<string, object>>();
            
            foreach (var obj in root.EnumerateArray())
            {
                var objDict = new Dictionary<string, object>();
                
                foreach (var prop in obj.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String)
                    {
                        objDict[prop.Name] = prop.Value.GetString();
                    }
                    else if (prop.Value.ValueKind == JsonValueKind.Number)
                    {
                        objDict[prop.Name] = prop.Value.GetDecimal();
                    }
                    else if (prop.Value.ValueKind == JsonValueKind.True || prop.Value.ValueKind == JsonValueKind.False)
                    {
                        objDict[prop.Name] = prop.Value.GetBoolean();
                    }
                    else if (prop.Value.ValueKind == JsonValueKind.Object || prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        objDict[prop.Name] = JsonSerializer.Deserialize<object>(prop.Value.GetRawText());
                    }
                    else
                    {
                        objDict[prop.Name] = prop.Value.GetRawText();
                    }
                }
                
                objects.Add(objDict);
            }
            
            return BuildReportDictionary(objects, key, isEvent);
        }
        else
        {
            // If not array, return raw JSON
            var report = new Dictionary<string, object>();
            report[isEvent ? "eventKey" : "chartKey"] = key;
            report["rawData"] = JsonSerializer.Deserialize<object>(jsonContent);
            return report;
        }
    }

    /// <summary>
    /// Helper method to build report dictionary with statistics
    /// </summary>
    private Dictionary<string, object> BuildReportDictionary(List<Dictionary<string, object>> objects, string key, bool isEvent)
    {
        var report = new Dictionary<string, object>();
        
        if (isEvent)
        {
            report["eventKey"] = key;
        }
        else
        {
            report["chartKey"] = key;
        }
        
        report["totalObjects"] = objects.Count;
        report["objects"] = objects;
        
        // Calculate summary statistics
        var categoryCounts = new Dictionary<string, int>();
        var typeCounts = new Dictionary<string, int>();
        
        if (isEvent)
        {
            var statusCounts = new Dictionary<string, int>();
            
            foreach (var obj in objects)
            {
                // Count by status (for events only)
                if (obj.TryGetValue("status", out var statusObj) && statusObj is string status)
                {
                    statusCounts[status] = statusCounts.GetValueOrDefault(status, 0) + 1;
                }
                
                // Count by category
                if (obj.TryGetValue("categoryKey", out var categoryObj) && categoryObj is string categoryKey)
                {
                    categoryCounts[categoryKey] = categoryCounts.GetValueOrDefault(categoryKey, 0) + 1;
                }
            }
            
            report["statusCounts"] = statusCounts;
        }
        else
        {
            foreach (var obj in objects)
            {
                // Count by category
                if (obj.TryGetValue("categoryKey", out var categoryObj) && categoryObj is string categoryKey)
                {
                    categoryCounts[categoryKey] = categoryCounts.GetValueOrDefault(categoryKey, 0) + 1;
                }
                
                // Count by object type (seat, table, etc.) - for charts
                if (obj.TryGetValue("type", out var typeObj) && typeObj is string type)
                {
                    typeCounts[type] = typeCounts.GetValueOrDefault(type, 0) + 1;
                }
            }
            
            report["typeCounts"] = typeCounts;
        }
        
        report["categoryCounts"] = categoryCounts;
        
        return report;
    }

    public async Task PublishDraftVersionAsync(string chartKey)
    {
        try
        {
            await client.Charts.PublishDraftVersionAsync(chartKey);
        }
        catch (Exception ex)
        {
            // If chart doesn't have a draft version, it might already be published
            // Try to retrieve the published version to verify
            if (ex.Message.Contains("does not have a draft version", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("no draft version", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    // Try to retrieve published version to verify chart exists and is published
                    await client.Charts.RetrievePublishedVersionAsync(chartKey);
                    // Chart is already published, no need to publish again
                    return;
                }
                catch
                {
                    // Chart doesn't exist or has other issues, rethrow original exception
                    throw new Exception($"Failed to publish draft version of chart {chartKey}: {ex.Message}", ex);
                }
            }
            throw new Exception($"Failed to publish draft version of chart {chartKey}: {ex.Message}", ex);
        }
    }

    public async Task<HoldTokenDto> CreateHoldTokenAsync(int expiresInSeconds)
    {
        try
        {
            // Use Seats.io SDK to create hold token
            var holdToken = await client.HoldTokens.CreateAsync(expiresInSeconds);
            
            return new HoldTokenDto
            {
                HoldToken = holdToken.Token,
                ExpiresAt = holdToken.ExpiresAt.UtcDateTime, // Convert to UTC DateTime for PostgreSQL compatibility
                ExpiresInSeconds = expiresInSeconds
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to create hold token: {ex.Message}", ex);
        }
    }

    public async Task HoldSeatsAsync(string eventKey, List<string> seatIds, string holdToken)
    {
        try
        {
            // Use Seats.io SDK to hold seats
            await client.Events.HoldAsync(eventKey, seatIds.ToArray(), holdToken);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to hold seats in Seats.io: {ex.Message}", ex);
        }
    }

    public async Task BookSeatsAsync(string eventKey, List<string> seatIds)
    {
        try
        {
            // Use Seats.io SDK to book seats permanently
            await client.Events.BookAsync(eventKey, seatIds.ToArray());
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to book seats in Seats.io: {ex.Message}", ex);
        }
    }

    public async Task ReleaseSeatsAsync(string eventKey, List<string> seatIds)
    {
        try
        {
            // Use Seats.io SDK to release held seats
            await client.Events.ReleaseAsync(eventKey, seatIds.ToArray());
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to release seats in Seats.io: {ex.Message}", ex);
        }
    }

    private string GenerateRandomColor()
    {
        var random = new Random();
        return $"#{random.Next(0x1000000):X6}"; 
    }
}
