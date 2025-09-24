using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Domain.Entities;
using Eventiq.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace Eventiq.Infrastructure.Identity;

public class EventItemRepository:GenericRepository<EventItem>, IEventItemRepository
{
    public EventItemRepository(ApplicationDbContext context, ILogger<EventItem> logger) : base(context, logger)
    {
    }
}