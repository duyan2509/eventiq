using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Eventiq.Infrastructure.Services;

/// <summary>
/// In-memory message queue service (tạm thời, có thể thay bằng RabbitMQ/MassTransit sau)
/// </summary>
public class InMemoryMessageQueueService : IMessageQueueService
{
    private readonly ILogger<InMemoryMessageQueueService> _logger;
    private static readonly Queue<EventProcessingMessage> _messageQueue = new();
    private static readonly object _lock = new();

    public InMemoryMessageQueueService(ILogger<InMemoryMessageQueueService> logger)
    {
        _logger = logger;
    }

    public Task PublishEventProcessingMessageAsync(EventProcessingMessage message)
    {
        lock (_lock)
        {
            _messageQueue.Enqueue(message);
            _logger.LogInformation("Event processing message queued for EventId: {EventId}", message.EventId);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Lấy message từ queue (dùng bởi worker/consumer)
    /// </summary>
    public static bool TryDequeue(out EventProcessingMessage? message)
    {
        lock (_lock)
        {
            return _messageQueue.TryDequeue(out message);
        }
    }

    /// <summary>
    /// Kiểm tra có message trong queue không
    /// </summary>
    public static int QueueCount
    {
        get
        {
            lock (_lock)
            {
                return _messageQueue.Count;
            }
        }
    }
}

