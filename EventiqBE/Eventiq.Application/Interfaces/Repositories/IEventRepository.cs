using Eventiq.Application.Dtos;
using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Repositories;

public interface IEventRepository:IGenericRepository<Event>
{
    Task<int> GetOrgEventCountAsync(Guid orgId);
    Task<PaginatedResult<Event>> GetByOrgAsync(Guid orgId, int page, int size);
    Task<PaginatedResult<Event>> GetAllAsync(int page, int size, EventStatus? status = null);
    Task<Event?> GetDetailEventAsync(Guid eventId);
}