using Eventiq.Application.Dtos;

namespace Eventiq.Application.Interfaces.Services;


public interface IMessageQueueService
{
    /// <summary>
    /// Gửi message để xử lý tạo seat map và vé cho event
    /// </summary>
    Task PublishEventProcessingMessageAsync(EventProcessingMessage message);
}

