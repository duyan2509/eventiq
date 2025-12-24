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
}