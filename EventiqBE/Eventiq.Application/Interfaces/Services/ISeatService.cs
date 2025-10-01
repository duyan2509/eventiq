using System.Text.Json;
using Eventiq.Application.Dtos;
using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Services;

public interface ISeatService
{
    Task<string> CreateChartAsync(IEnumerable<TicketClass> ticketClass);
}