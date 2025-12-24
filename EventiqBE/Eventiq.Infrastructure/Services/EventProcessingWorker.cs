using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces;
using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Application.Interfaces.Services;
using Eventiq.Domain.Entities;
using Eventiq.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Eventiq.Infrastructure.Services;

/// <summary>
/// Background worker để xử lý message queue và tạo seat map, vé
/// </summary>
public class EventProcessingWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventProcessingWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(5); // Check queue mỗi 5 giây

    public EventProcessingWorker(IServiceProvider serviceProvider, ILogger<EventProcessingWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EventProcessingWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Kiểm tra queue có message không
                if (InMemoryMessageQueueService.TryDequeue(out var message))
                {
                    _logger.LogInformation("Processing event: {EventId}", message!.EventId);
                    
                    // Tạo scope để inject dependencies
                    using var scope = _serviceProvider.CreateScope();
                    await ProcessEventAsync(message, scope, stoppingToken);
                }
                else
                {
                    // Không có message, đợi 
                    await Task.Delay(_checkInterval, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EventProcessingWorker");
                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        _logger.LogInformation("EventProcessingWorker stopped");
    }

    private async Task ProcessEventAsync(EventProcessingMessage message, IServiceScope scope, CancellationToken cancellationToken)
    {
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var chartRepository = scope.ServiceProvider.GetRequiredService<IChartRepository>();
        var eventSeatRepository = scope.ServiceProvider.GetRequiredService<IEventSeatRepository>();
        var eventSeatStateRepository = scope.ServiceProvider.GetRequiredService<IEventSeatStateRepository>();
        var eventItemRepository = scope.ServiceProvider.GetRequiredService<IEventItemRepository>();
        var eventApprovalHistoryRepository = scope.ServiceProvider.GetRequiredService<IEventApprovalHistoryRepository>();
        var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        var seatService = scope.ServiceProvider.GetRequiredService<ISeatService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            await unitOfWork.BeginTransactionAsync();

            // Lấy event
            var evnt = await eventRepository.GetByIdAsync(message.EventId);
            if (evnt == null)
            {
                _logger.LogWarning("Event {EventId} not found", message.EventId);
                return;
            }

            // Lấy tất cả charts của event
            var charts = await chartRepository.GetByEventItAsync(message.EventId);
            
            foreach (var chart in charts)
            {
                // Lấy venue definition từ Seats.io API khi admin approve
                // Data đã được lưu trong Seats.io bởi designer
                try
                {
                    string? venueDefinitionJson = null;
                    
                    // Nếu chart đã có venue definition trong DB, dùng nó (fallback)
                    if (!string.IsNullOrEmpty(chart.VenueDefinition))
                    {
                        _logger.LogInformation("Using existing venue definition from DB for chart {ChartId}", chart.Id);
                        venueDefinitionJson = chart.VenueDefinition;
                    }
                    else
                    {
                        // Thử lấy từ Seats.io API
                        try
                        {
                            venueDefinitionJson = await seatService.GetVenueDefinitionFromChartAsync(chart.Key);
                            _logger.LogInformation("Retrieved venue definition from Seats.io for chart {ChartId}", chart.Id);
                        }
                        catch (Exception apiEx)
                        {
                            _logger.LogWarning(apiEx, "Failed to retrieve venue definition from Seats.io for chart {ChartId}, chart may not exist or be accessible", chart.Id);
                            // Nếu không lấy được từ API, skip chart này nhưng không fail toàn bộ event
                            continue;
                        }
                    }
                    
                    if (string.IsNullOrEmpty(venueDefinitionJson))
                    {
                        _logger.LogWarning("No venue definition available for chart {ChartId}, skipping", chart.Id);
                        continue;
                    }
                    
                    // Lưu venue definition vào chart (để reference sau này)
                    chart.VenueDefinition = venueDefinitionJson;
                    await chartRepository.UpdateAsync(chart);
                    
                    // Process seats từ venue definition
                    await ProcessChartFromVenueDefinitionAsync(
                        chart, 
                        eventSeatRepository, 
                        eventSeatStateRepository, 
                        eventItemRepository,
                        message.EventId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing chart {ChartId} from Seats.io", chart.Id);
                    // Không throw để không rollback toàn bộ event, chỉ log và continue
                    // Nếu muốn strict, có thể uncomment dòng throw
                    // throw;
                }
            }

            var previousStatus = evnt.Status;
            
            evnt.Status = EventStatus.Published;
            await eventRepository.UpdateAsync(evnt);

            try
            {
                var adminUser = await identityService.GetByIdAsync(message.AdminUserId);
                var history = new EventApprovalHistory
                {
                    EventId = message.EventId,
                    PreviousStatus = previousStatus,
                    NewStatus = EventStatus.Published,
                    Comment = "Event processed successfully - seats and tickets created",
                    ApprovedByUserId = message.AdminUserId,
                    ApprovedByUserName = adminUser?.Email ?? "System",
                    ActionDate = DateTime.UtcNow
                };
                await eventApprovalHistoryRepository.AddAsync(history);
                _logger.LogInformation("Created approval history for event {EventId}: {PreviousStatus} -> Published", message.EventId, previousStatus);
            }
            catch (Exception historyEx)
            {
                _logger.LogWarning(historyEx, "Failed to create approval history for event {EventId}", message.EventId);
            }

            await unitOfWork.CommitAsync();
            _logger.LogInformation("Successfully processed event {EventId}", message.EventId);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Error processing event {EventId}", message.EventId);
            
            try
            {
                var evnt = await eventRepository.GetByIdAsync(message.EventId);
                if (evnt != null)
                {
                    evnt.Status = EventStatus.Pending; // Hoặc có thể reject
                    await eventRepository.UpdateAsync(evnt);
                }
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Error rolling back event status");
            }
        }
    }

    private async Task ProcessChartFromVenueDefinitionAsync(
        Chart chart,
        IEventSeatRepository eventSeatRepository,
        IEventSeatStateRepository eventSeatStateRepository,
        IEventItemRepository eventItemRepository,
        Guid eventId)
    {
        try
        {
            // Parse venue definition JSON
            using var doc = System.Text.Json.JsonDocument.Parse(chart.VenueDefinition!);
            var root = doc.RootElement;
            
            if (!root.TryGetProperty("objects", out var objectsElement))
            {
                _logger.LogWarning("Invalid venue definition for chart {ChartId}: missing 'objects'", chart.Id);
                return;
            }

            var eventSeats = new List<EventSeat>();

            // Extract seats từ venue definition
            foreach (var objProperty in objectsElement.EnumerateObject())
            {
                var obj = objProperty.Value;
                
                if (!obj.TryGetProperty("type", out var typeElement))
                    continue;
                
                var objType = typeElement.GetString();
                if (objType != "seat" && objType != "table")
                    continue;

                var seat = new EventSeat
                {
                    ChartId = chart.Id,
                    SeatKey = objProperty.Name,
                    Label = obj.TryGetProperty("label", out var labelElement) 
                        ? labelElement.GetString() 
                        : objProperty.Name,
                    Section = obj.TryGetProperty("section", out var sectionElement) 
                        ? sectionElement.GetString() 
                        : null,
                    Row = obj.TryGetProperty("row", out var rowElement) 
                        ? rowElement.GetString() 
                        : null,
                    Number = obj.TryGetProperty("number", out var numberElement) 
                        ? numberElement.GetString() 
                        : null,
                    CategoryKey = obj.TryGetProperty("categoryKey", out var categoryKeyElement) 
                        ? categoryKeyElement.GetString() 
                        : (obj.TryGetProperty("category", out var categoryElement) 
                            ? categoryElement.GetString() 
                            : null),
                    ExtraData = obj.TryGetProperty("extraData", out var extraDataElement) 
                        ? extraDataElement.GetRawText() 
                        : null
                };

                eventSeats.Add(seat);
            }

            // Bulk upsert seats
            if (eventSeats.Any())
            {
                await eventSeatRepository.BulkUpsertAsync(eventSeats);
                _logger.LogInformation("Created {Count} seats for chart {ChartId}", eventSeats.Count, chart.Id);
            }

            // Tạo EventSeatState cho tất cả event items sử dụng chart này
            var eventItems = await eventItemRepository.GetAllByEventIdAsync(eventId);
            var chartEventItems = eventItems.Where(ei => ei.ChartId == chart.Id).ToList();
            
            var allSeats = await eventSeatRepository.GetByChartIdAsync(chart.Id);
            
            foreach (var eventItem in chartEventItems)
            {
                foreach (var seat in allSeats)
                {
                    var existingState = await eventSeatStateRepository.GetByEventItemAndSeatAsync(eventItem.Id, seat.Id);
                    if (existingState == null)
                    {
                        var newState = new EventSeatState
                        {
                            EventItemId = eventItem.Id,
                            EventSeatId = seat.Id,
                            Status = SeatStatus.Free
                        };
                        await eventSeatStateRepository.AddAsync(newState);
                    }
                }
            }

            _logger.LogInformation("Created seat states for {Count} event items", chartEventItems.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chart {ChartId} from venue definition", chart.Id);
            throw;
        }
    }
}

