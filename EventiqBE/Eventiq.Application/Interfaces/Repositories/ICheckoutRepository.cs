using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Repositories;

public interface ICheckoutRepository : IGenericRepository<Checkout>
{
    Task<Checkout?> GetByCheckoutIdWithEventItemAsync(Guid checkoutId);
    Task<IEnumerable<Checkout>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<Checkout>> GetByEventItemIdAsync(Guid eventItemId);
}

