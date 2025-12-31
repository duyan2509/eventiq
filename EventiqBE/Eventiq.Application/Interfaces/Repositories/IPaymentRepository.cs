using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Repositories;

public interface IPaymentRepository : IGenericRepository<Payment>
{
    Task<Payment?> GetByPaymentIdAsync(string paymentId);
    Task<Payment?> GetByCheckoutIdAsync(Guid checkoutId);
    Task<IEnumerable<Payment>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<Payment>> GetByEventItemIdAsync(Guid eventItemId);
    Task<IEnumerable<Payment>> GetSuccessfulPaymentsByEventItemIdAsync(Guid eventItemId);
}

