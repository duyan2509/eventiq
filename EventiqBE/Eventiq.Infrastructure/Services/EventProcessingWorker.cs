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
/// Background worker 
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
                var queueCount = InMemoryMessageQueueService.QueueCount;
                if (queueCount > 0)
                {
                    _logger.LogInformation("Queue has {Count} message(s) waiting", queueCount);
                }
                
                if (InMemoryMessageQueueService.TryDequeue(out var message))
                {
                    _logger.LogInformation("Dequeued message for event: {EventId}. Starting processing...", message!.EventId);
                    
                    // Tạo scope để inject dependencies
                    using var scope = _serviceProvider.CreateScope();
                    await ProcessEventAsync(message, scope, stoppingToken);
                    
                    _logger.LogInformation("Completed processing event: {EventId}", message.EventId);
                }
                else
                {
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
        _logger.LogInformation("ProcessEventAsync started for event {EventId}", message.EventId);
        
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
            _logger.LogInformation("Beginning transaction for event {EventId}", message.EventId);
            await unitOfWork.BeginTransactionAsync();

            // get event
            var evnt = await eventRepository.GetByIdAsync(message.EventId);
            if (evnt == null)
            {
                _logger.LogWarning("Event {EventId} not found", message.EventId);
                await unitOfWork.RollbackAsync();
                return;
            }

            // get all event items of event
            var eventItems = await eventItemRepository.GetAllByEventIdAsync(message.EventId);
            
            // Group event items by chartId to avoid duplicate API calls
            var eventItemsByChart = eventItems
                .Where(ei => ei.ChartId != Guid.Empty)
                .GroupBy(ei => ei.ChartId)
                .ToList();
            
            // Process seats by chart (one API call per chart)
            foreach (var chartGroup in eventItemsByChart)
            {
                var chartId = chartGroup.Key;
                var chartEventItems = chartGroup.ToList();
                
                try
                {
                    _logger.LogInformation("Processing {Count} event items for chart {ChartId}", chartEventItems.Count, chartId);
                    
                    // Get chart
                    var chart = await chartRepository.GetByIdAsync(chartId);
                    if (chart == null)
                    {
                        _logger.LogWarning("Chart {ChartId} not found, skipping {Count} event items", chartId, chartEventItems.Count);
                        continue;
                    }
                    
                    if (string.IsNullOrEmpty(chart.Key))
                    {
                        _logger.LogWarning("Chart {ChartId} has no chart key, skipping {Count} event items", chartId, chartEventItems.Count);
                        continue;
                    }
                    
                    // Get chart report once for this chart
                    _logger.LogInformation("Retrieving chart report for chart {ChartKey} (chartId: {ChartId})", chart.Key, chart.Id);
                    
                    object? chartReport = null;
                    try
                    {
                        chartReport = await seatService.GetChartReportDetailAsync(chart.Key);
                        _logger.LogInformation("Successfully retrieved chart report for chart {ChartKey}", chart.Key);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to retrieve chart report for chart {ChartKey} (chartId: {ChartId})", chart.Key, chart.Id);
                        // Skip all event items for this chart
                        continue;
                    }
                    
                    // Process chart report and create seats for all event items using this chart
                    await ProcessChartReportForMultipleEventItemsAsync(
                        chart,
                        chartEventItems,
                        chartReport,
                        eventSeatRepository,
                        eventSeatStateRepository);
                    
                    _logger.LogInformation("Successfully processed chart {ChartKey} for {Count} event items", chart.Key, chartEventItems.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing chart {ChartId} for {Count} event items", chartId, chartEventItems.Count);
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
            _logger.LogError(ex, "Error processing event {EventId}. Rolling back transaction.", message.EventId);
            
            try
            {
                await unitOfWork.RollbackAsync();
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Error rolling back transaction for event {EventId}", message.EventId);
            }
            
            try
            {
                using var statusScope = _serviceProvider.CreateScope();
                var statusEventRepository = statusScope.ServiceProvider.GetRequiredService<IEventRepository>();
                var statusUnitOfWork = statusScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                
                await statusUnitOfWork.BeginTransactionAsync();
                var evnt = await statusEventRepository.GetByIdAsync(message.EventId);
                if (evnt != null && evnt.Status == EventStatus.InProgress)
                {
                    evnt.Status = EventStatus.Pending;
                    await statusEventRepository.UpdateAsync(evnt);
                    await statusUnitOfWork.CommitAsync();
                    _logger.LogInformation("Updated event {EventId} status from InProgress to Pending after error", message.EventId);
                }
                else
                {
                    await statusUnitOfWork.RollbackAsync();
                }
            }
            catch (Exception statusEx)
            {
                _logger.LogError(statusEx, "Error updating event status back to Pending for event {EventId}", message.EventId);
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

            foreach (var objProperty in objectsElement.EnumerateObject())
            {
                var obj = objProperty.Value;
                
                if (!obj.TryGetProperty("type", out var typeElement))
                    continue;
                
                var objType = typeElement.GetString();
                if (objType != "seat" && objType != "table")
                    continue;

                var seatLabel = obj.TryGetProperty("label", out var labelElement) 
                    ? labelElement.GetString() 
                    : objProperty.Name;
                
                if (string.IsNullOrEmpty(seatLabel))
                {
                    _logger.LogWarning("Skipping object with no label in chart {ChartId}", chart.Id);
                    continue;
                }
                
                var seat = new EventSeat
                {
                    ChartId = chart.Id,
                    Label = seatLabel, // Label is now the unique identifier
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

            var eventItems = await eventItemRepository.GetAllByEventIdAsync(eventId);
            var chartEventItems = eventItems.Where(ei => ei.ChartId == chart.Id).ToList();
            
            var allSeats = await eventSeatRepository.GetByChartIdAsync(chart.Id);
            var seatIds = allSeats.Select(s => s.Id).ToList();
            
            foreach (var eventItem in chartEventItems)
            {
                // Batch load existing states to avoid N+1 queries
                var existingStatesMap = await eventSeatStateRepository.GetSeatStatusMapAsync(eventItem.Id, seatIds);
                var existingSeatIds = existingStatesMap.Keys.ToHashSet();

                var newStates = new List<EventSeatState>();
                foreach (var seat in allSeats)
                {
                    if (!existingSeatIds.Contains(seat.Id))
                    {
                        newStates.Add(new EventSeatState
                        {
                            EventItemId = eventItem.Id,
                            EventSeatId = seat.Id,
                            Status = SeatStatus.Free
                        });
                    }
                }

                if (newStates.Any())
                {
                    await eventSeatStateRepository.AddRangeAsync(newStates);
                    _logger.LogInformation("Created {Count} seat states for event item {EventItemId}", newStates.Count, eventItem.Id);
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


    private async Task ProcessEventItemFromEventAsync(
        EventItem eventItem,
        IEnumerable<SeatInfoDto> seats,
        IEventSeatRepository eventSeatRepository,
        IEventSeatStateRepository eventSeatStateRepository)
    {
        try
        {
            var eventSeats = new List<EventSeat>();

            // Convert SeatInfoDto to EventSeat
            foreach (var seatDto in seats)
            {
                var seatLabel = seatDto.Label ?? seatDto.SeatKey ?? throw new InvalidOperationException("Seat must have either Label or SeatKey");
                var existingSeat = await eventSeatRepository.GetSeatByLabelAsync(eventItem.ChartId, seatLabel);
                
                if (existingSeat == null)
                {
                    var seat = new EventSeat
                    {
                        ChartId = eventItem.ChartId,
                        Label = seatLabel, // Label is now the unique identifier
                        Section = seatDto.Section,
                        Row = seatDto.Row,
                        Number = seatDto.Number,
                        CategoryKey = seatDto.CategoryKey,
                        ExtraData = seatDto.ExtraData != null 
                            ? System.Text.Json.JsonSerializer.Serialize(seatDto.ExtraData) 
                            : null
                    };
                    eventSeats.Add(seat);
                }
                else
                {
                    existingSeat.Label = seatDto.Label ?? existingSeat.Label;
                    existingSeat.Section = seatDto.Section ?? existingSeat.Section;
                    existingSeat.Row = seatDto.Row ?? existingSeat.Row;
                    existingSeat.Number = seatDto.Number ?? existingSeat.Number;
                    existingSeat.CategoryKey = seatDto.CategoryKey ?? existingSeat.CategoryKey;
                    if (seatDto.ExtraData != null)
                    {
                        existingSeat.ExtraData = System.Text.Json.JsonSerializer.Serialize(seatDto.ExtraData);
                    }
                    eventSeats.Add(existingSeat);
                }
            }

            // Bulk upsert seats
            if (eventSeats.Any())
            {
                await eventSeatRepository.BulkUpsertAsync(eventSeats);
                _logger.LogInformation("Created/updated {Count} seats for event item {EventItemId}", eventSeats.Count, eventItem.Id);
            }

            var allSeats = await eventSeatRepository.GetByChartIdAsync(eventItem.ChartId);
            var seatIds = allSeats.Select(s => s.Id).ToList();

            var existingStatesMap = await eventSeatStateRepository.GetSeatStatusMapAsync(eventItem.Id, seatIds);
            var existingSeatIds = existingStatesMap.Keys.ToHashSet();

            var newStates = new List<EventSeatState>();
            foreach (var seat in allSeats)
            {
                if (!existingSeatIds.Contains(seat.Id))
                {
                    newStates.Add(new EventSeatState
                    {
                        EventItemId = eventItem.Id,
                        EventSeatId = seat.Id,
                        Status = SeatStatus.Free
                    });
                }
            }

            if (newStates.Any())
            {
                await eventSeatStateRepository.AddRangeAsync(newStates);
                _logger.LogInformation("Created {Count} seat states for event item {EventItemId}", newStates.Count, eventItem.Id);
            }
            else
            {
                _logger.LogInformation("All seats already have states for event item {EventItemId}", eventItem.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event item {EventItemId} from event", eventItem.Id);
            throw;
        }
    }


    private async Task ProcessEventItemFromChartAsync(
        EventItem eventItem,
        IChartRepository chartRepository,
        ISeatService seatService,
        IEventSeatRepository eventSeatRepository,
        IEventSeatStateRepository eventSeatStateRepository,
        IEventItemRepository eventItemRepository)
    {
        try
        {
            var chart = await chartRepository.GetByIdAsync(eventItem.ChartId);
            if (chart == null)
            {
                _logger.LogWarning("Chart {ChartId} not found for event item {EventItemId}", eventItem.ChartId, eventItem.Id);
                return;
            }

            if (string.IsNullOrEmpty(chart.Key))
            {
                _logger.LogWarning("Chart {ChartId} has no chart key for event item {EventItemId}", chart.Id, eventItem.Id);
                return;
            }

            // Use chart report API to get all objects
            _logger.LogInformation("Retrieving chart report for chart {ChartKey} (chartId: {ChartId}) for event item {EventItemId}", 
                chart.Key, chart.Id, eventItem.Id);
            
            try
            {
                var chartReport = await seatService.GetChartReportDetailAsync(chart.Key);
                
                await ProcessChartReportAsync(
                    chart,
                    eventItem,
                    chartReport,
                    eventSeatRepository,
                    eventSeatStateRepository);
                
                _logger.LogInformation("Successfully processed chart report for event item {EventItemId}", eventItem.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve or process chart report for chart {ChartKey} (chartId: {ChartId}) for event item {EventItemId}", 
                    chart.Key, chart.Id, eventItem.Id);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event item {EventItemId} from chart", eventItem.Id);
            throw;
        }
    }


    private async Task ProcessChartReportForMultipleEventItemsAsync(
        Chart chart,
        List<EventItem> eventItems,
        object chartReport,
        IEventSeatRepository eventSeatRepository,
        IEventSeatStateRepository eventSeatStateRepository)
    {
        try
        {
            // Parse chart report response
            var reportJson = System.Text.Json.JsonSerializer.Serialize(chartReport);
            using var doc = System.Text.Json.JsonDocument.Parse(reportJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("objects", out var objectsElement))
            {
                _logger.LogWarning("Chart report for chart {ChartId} has no objects", chart.Id);
                return;
            }

            var eventSeats = new List<EventSeat>();

            // Parse each object from chart report
            foreach (var obj in objectsElement.EnumerateArray())
            {
                try
                {
                    var label = obj.TryGetProperty("Label", out var labelElement) 
                        ? labelElement.GetString() 
                        : null;
                    
                    var objectType = obj.TryGetProperty("ObjectType", out var typeElement) 
                        ? typeElement.GetString() 
                        : null;
                    
                    if (objectType != "seat" && objectType != "table" && objectType != "generalAdmission")
                        continue;

                    if (string.IsNullOrEmpty(label))
                    {
                        _logger.LogWarning("Skipping object with no label in chart {ChartId}", chart.Id);
                        continue;
                    }

                    // Get Labels for row/parent info
                    string? row = null;
                    string? section = null;
                    string? number = null;
                    
                    if (obj.TryGetProperty("Labels", out var labelsElement))
                    {
                        if (labelsElement.TryGetProperty("Parent", out var parentElement))
                        {
                            if (parentElement.TryGetProperty("Label", out var parentLabelElement))
                            {
                                row = parentLabelElement.GetString();
                            }
                        }
                        
                        if (labelsElement.TryGetProperty("Section", out var sectionElement))
                        {
                            section = sectionElement.GetString();
                        }
                        
                        if (labelsElement.TryGetProperty("Own", out var ownLabelElement))
                        {
                            if (ownLabelElement.TryGetProperty("Label", out var ownLabelValueElement))
                            {
                                number = ownLabelValueElement.GetString();
                            }
                        }
                    }
                    
                    if (string.IsNullOrEmpty(section))
                    {
                        section = obj.TryGetProperty("Section", out var sectionDirectElement) 
                            ? sectionDirectElement.GetString() 
                            : null;
                    }

                    var categoryKey = obj.TryGetProperty("CategoryKey", out var categoryKeyElement) 
                        ? categoryKeyElement.GetString() 
                        : null;

                    var seat = new EventSeat
                    {
                        ChartId = chart.Id,
                        Label = label, // Label is required and unique
                        Section = section,
                        Row = row,
                        Number = number,
                        CategoryKey = categoryKey,
                        ExtraData = obj.GetRawText() // Store full object as JSON
                    };

                    eventSeats.Add(seat);
                }
                catch (Exception objEx)
                {
                    _logger.LogWarning(objEx, "Error parsing object in chart report for chart {ChartId}", chart.Id);
                }
            }

            // Bulk upsert seats (one time for the chart)
            if (eventSeats.Any())
            {
                await eventSeatRepository.BulkUpsertAsync(eventSeats);
                _logger.LogInformation("Created/updated {Count} seats for chart {ChartId}", 
                    eventSeats.Count, chart.Id);
            }
            else
            {
                _logger.LogWarning("No seats found in chart report for chart {ChartId}", chart.Id);
                return;
            }

            var allSeats = await eventSeatRepository.GetByChartIdAsync(chart.Id);
            var seatIds = allSeats.Select(s => s.Id).ToList();

            foreach (var eventItem in eventItems)
            {
                try
                {
                    var existingStatesMap = await eventSeatStateRepository.GetSeatStatusMapAsync(eventItem.Id, seatIds);
                    var existingSeatIds = existingStatesMap.Keys.ToHashSet();

                    var newStates = new List<EventSeatState>();
                    foreach (var seat in allSeats)
                    {
                        if (!existingSeatIds.Contains(seat.Id))
                        {
                            newStates.Add(new EventSeatState
                            {
                                EventItemId = eventItem.Id,
                                EventSeatId = seat.Id,
                                Status = SeatStatus.Free
                            });
                        }
                    }

                    // Bulk insert new states
                    if (newStates.Any())
                    {
                        await eventSeatStateRepository.AddRangeAsync(newStates);
                        _logger.LogInformation("Created {Count} seat states for event item {EventItemId}", newStates.Count, eventItem.Id);
                    }
                    else
                    {
                        _logger.LogInformation("All seats already have states for event item {EventItemId}", eventItem.Id);
                    }
                }
                catch (Exception itemEx)
                {
                    _logger.LogError(itemEx, "Error creating seat states for event item {EventItemId}", eventItem.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chart report for chart {ChartId} (event items: {Count})", 
                chart.Id, eventItems.Count);
            throw;
        }
    }

    private async Task ProcessChartReportAsync(
        Chart chart,
        EventItem eventItem,
        object chartReport,
        IEventSeatRepository eventSeatRepository,
        IEventSeatStateRepository eventSeatStateRepository)
    {
        try
        {
            // Parse chart report response
            var reportJson = System.Text.Json.JsonSerializer.Serialize(chartReport);
            using var doc = System.Text.Json.JsonDocument.Parse(reportJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("objects", out var objectsElement))
            {
                _logger.LogWarning("Chart report for chart {ChartId} has no objects", chart.Id);
                return;
            }

            var eventSeats = new List<EventSeat>();

            // Parse each object from chart report
            foreach (var obj in objectsElement.EnumerateArray())
            {
                try
                {
                    var label = obj.TryGetProperty("Label", out var labelElement) 
                        ? labelElement.GetString() 
                        : null;
                    
                    var objectType = obj.TryGetProperty("ObjectType", out var typeElement) 
                        ? typeElement.GetString() 
                        : null;
                    
                    if (objectType != "seat" && objectType != "table" && objectType != "generalAdmission")
                        continue;

                    // Label is now the unique identifier (required)
                    if (string.IsNullOrEmpty(label))
                    {
                        _logger.LogWarning("Skipping object with no label in chart {ChartId}", chart.Id);
                        continue;
                    }

                    string? row = null;
                    string? section = null;
                    string? number = null;
                    
                    if (obj.TryGetProperty("Labels", out var labelsElement))
                    {
                        if (labelsElement.TryGetProperty("Parent", out var parentElement))
                        {
                            if (parentElement.TryGetProperty("Label", out var parentLabelElement))
                            {
                                row = parentLabelElement.GetString();
                            }
                        }
                        
                        if (labelsElement.TryGetProperty("Section", out var sectionElement))
                        {
                            section = sectionElement.GetString();
                        }
                        
                        if (labelsElement.TryGetProperty("Own", out var ownLabelElement))
                        {
                            if (ownLabelElement.TryGetProperty("Label", out var ownLabelValueElement))
                            {
                                number = ownLabelValueElement.GetString();
                            }
                        }
                    }
                    
                    if (string.IsNullOrEmpty(section))
                    {
                        section = obj.TryGetProperty("Section", out var sectionDirectElement) 
                            ? sectionDirectElement.GetString() 
                            : null;
                    }

                    // Get category
                    var categoryKey = obj.TryGetProperty("CategoryKey", out var categoryKeyElement) 
                        ? categoryKeyElement.GetString() 
                        : null;

                    var seat = new EventSeat
                    {
                        ChartId = chart.Id,
                        Label = label, // Label is required and unique
                        Section = section,
                        Row = row,
                        Number = number,
                        CategoryKey = categoryKey,
                        ExtraData = obj.GetRawText() // Store full object as JSON
                    };

                    eventSeats.Add(seat);
                }
                catch (Exception objEx)
                {
                    _logger.LogWarning(objEx, "Error parsing object in chart report for chart {ChartId}", chart.Id);
                    // Continue with next object
                }
            }

            if (eventSeats.Any())
            {
                await eventSeatRepository.BulkUpsertAsync(eventSeats);
                _logger.LogInformation("Created/updated {Count} seats for chart {ChartId} (event item {EventItemId})", 
                    eventSeats.Count, chart.Id, eventItem.Id);
            }
            else
            {
                _logger.LogWarning("No seats found in chart report for chart {ChartId} (event item {EventItemId})", 
                    chart.Id, eventItem.Id);
                return;
            }

            var allSeats = await eventSeatRepository.GetByChartIdAsync(chart.Id);
            var seatIds = allSeats.Select(s => s.Id).ToList();

            var existingStatesMap = await eventSeatStateRepository.GetSeatStatusMapAsync(eventItem.Id, seatIds);
            var existingSeatIds = existingStatesMap.Keys.ToHashSet();

            var newStates = new List<EventSeatState>();
            foreach (var seat in allSeats)
            {
                if (!existingSeatIds.Contains(seat.Id))
                {
                    newStates.Add(new EventSeatState
                    {
                        EventItemId = eventItem.Id,
                        EventSeatId = seat.Id,
                        Status = SeatStatus.Free
                    });
                }
            }

            // Bulk insert new states
            if (newStates.Any())
            {
                await eventSeatStateRepository.AddRangeAsync(newStates);
                _logger.LogInformation("Created {Count} seat states for event item {EventItemId}", newStates.Count, eventItem.Id);
            }
            else
            {
                _logger.LogInformation("All seats already have states for event item {EventItemId}", eventItem.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chart report for chart {ChartId} (event item {EventItemId})", 
                chart.Id, eventItem.Id);
            throw;
        }
    }
}

