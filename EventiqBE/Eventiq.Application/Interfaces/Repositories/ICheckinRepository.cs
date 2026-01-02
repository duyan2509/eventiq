using Eventiq.Application.Dtos;
using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Repositories;

public interface ICheckinRepository : IGenericRepository<Checkin>
{
    Task<IEnumerable<Checkin>> GetByEventItemIdAsync(Guid eventItemId);
    Task<PaginatedResult<Checkin>> GetByEventItemIdPaginatedAsync(Guid eventItemId, int page, int size);
}
