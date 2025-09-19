using System.Text.Json;

namespace Eventiq.Application.Interfaces.Services;

public interface ISeatService
{
    Task<JsonDocument> BookSeats(string eventKey, List<string> seats);
    Task<JsonDocument> HoldSeats(string eventKey, List<string> seats);
    Task<string> CreateHoldToken();
    Task<JsonDocument> ReleaseSeats(string eventKey, List<string> seats);
    Task<JsonDocument> CreateEvent(string chartKey);
}