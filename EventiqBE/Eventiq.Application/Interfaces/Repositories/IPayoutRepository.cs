using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Repositories;

public interface IPayoutRepository : IGenericRepository<Payout>
{
    Task<Payout?> GetByEventItemIdAsync(Guid eventItemId);
    Task<IEnumerable<Payout>> GetByEventIdAsync(Guid eventId);
    Task<IEnumerable<Payout>> GetByOrganizationIdAsync(Guid organizationId);
    Task<IEnumerable<Payout>> GetPendingPayoutsAsync();
    Task<IEnumerable<Payout>> GetPaidPayoutsAsync();
    Task<IEnumerable<Payout>> GetPayoutsByFiltersAsync(PayoutStatus? status, int? month, int? year);
}

