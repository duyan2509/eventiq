using Eventiq.Application.Interfaces.Services;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Eventiq.Application.Dtos;
using Eventiq.Domain.Entities;
using Microsoft.Extensions.Configuration;
using SeatsioDotNet;
using SeatsioDotNet.Charts;

namespace Eventiq.Infrastructure.Services;

public class SeatsIoService : ISeatService
{
    private readonly SeatsioClient client;

    public SeatsIoService(string? secretKey)
    {
        client = new SeatsioClient(Region.OC(), secretKey);
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

    private string GenerateRandomColor()
    {
        var random = new Random();
        return $"#{random.Next(0x1000000):X6}"; 
    }



    
}
