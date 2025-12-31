using System.Text.Json;
using Eventiq.Application.Dtos;
using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Services;

public interface ISeatService
{
    Task<string> CreateChartAsync(IEnumerable<TicketClass> ticketClass);
    Task<IEnumerable<SeatInfoDto>> GetSeatsFromChartAsync(string chartKey);
    Task<Dictionary<string, string>> GetSeatStatusForEventItemAsync(string chartKey, Guid eventItemId, IEnumerable<string> seatKeys);

    Task<string> GetVenueDefinitionFromChartAsync(string chartKey);

    Task<string> CreateEventFromChartAsync(string chartKey, string? eventKey = null);

    Task<SeatsIoEventDto> RetrieveEventAsync(string eventKey);

    Task<IEnumerable<SeatInfoDto>> GetAllObjectsFromEventAsync(string eventKey, int maxRetries = 3, int delayMs = 1000);

    Task<object> RetrievePublishedChartVersionAsync(string chartKey);

    Task PublishDraftVersionAsync(string chartKey);

    Task<object> GetEventReportDetailAsync(string eventKey);

    Task<object> GetChartReportDetailAsync(string chartKey);
    

    Task<HoldTokenDto> CreateHoldTokenAsync(int expiresInSeconds);
    

    Task HoldSeatsAsync(string eventKey, List<string> seatIds, string holdToken);
    

    Task BookSeatsAsync(string eventKey, List<string> seatIds);

    Task ReleaseSeatsAsync(string eventKey, List<string> seatIds);
}