using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Repositories;

public interface IChartRepository:IGenericRepository<Chart>
{
    Task<IEnumerable<Chart>> GetByEventItAsync(Guid eventId);
    Task<Chart?> GetDetailChartByIdAsync(Guid chartId);
}