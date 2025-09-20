using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Repositories;

public interface IEventRepository:IGenericRepository<Event>
{
    Task<int> GetOrgEventCountAsync(Guid orgId);
}