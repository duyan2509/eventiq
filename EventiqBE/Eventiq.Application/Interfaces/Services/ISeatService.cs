using System.Text.Json;
using Eventiq.Application.Dtos;
using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Services;

public interface ISeatService
{
    Task<string> CreateChartAsync(IEnumerable<TicketClass> ticketClass);
    Task<IEnumerable<SeatInfoDto>> GetSeatsFromChartAsync(string chartKey);
    Task<Dictionary<string, string>> GetSeatStatusForEventItemAsync(string chartKey, Guid eventItemId, IEnumerable<string> seatKeys);
    /// <summary>
    /// get venue definition from  Seats.io chart 
    /// </summary>
    Task<string> GetVenueDefinitionFromChartAsync(string chartKey);
    /// <summary>
    /// Create Seats.io event from chart key
    /// </summary>
    Task<string> CreateEventFromChartAsync(string chartKey, string? eventKey = null);
    /// <summary>
    /// Retrieve Seats.io event object by event key
    /// </summary>
    Task<SeatsIoEventDto> RetrieveEventAsync(string eventKey);
    /// <summary>
    /// Get all objects (seats) from Seats.io event using Event Reports API
    /// API: GET /events/{eventKey}/reports/byLabel
    /// </summary>
    Task<IEnumerable<SeatInfoDto>> GetAllObjectsFromEventAsync(string eventKey, int maxRetries = 3, int delayMs = 1000);
    /// <summary>
    /// Retrieve the published version of a chart (the actual drawing, containing the venue type, categories etc.)
    /// </summary>
    Task<object> RetrievePublishedChartVersionAsync(string chartKey);
    /// <summary>
    /// Publish the draft version of a chart to make it available for events
    /// </summary>
    Task PublishDraftVersionAsync(string chartKey);
    /// <summary>
    /// Get event report detail from Seats.io
    /// Returns detailed information about all objects (seats) in the event including status, category, etc.
    /// </summary>
    Task<object> GetEventReportDetailAsync(string eventKey);
    /// <summary>
    /// Get detailed chart report from Seats.io
    /// Returns detailed information about all objects (seats) in the chart including category, type, etc.
    /// Note: Chart reports show the structure of the chart, not the actual booking status (which is in events)
    /// </summary>
    Task<object> GetChartReportDetailAsync(string chartKey);
}