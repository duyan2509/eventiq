using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Repositories;

public interface IStaffInvitationRepository : IGenericRepository<StaffInvitation>
{
    Task<IEnumerable<StaffInvitation>> GetByEventIdAsync(Guid eventId);
    Task<IEnumerable<StaffInvitation>> GetByInvitedUserIdAsync(Guid userId);
    Task<StaffInvitation?> GetPendingByEventIdAndUserIdAsync(Guid eventId, Guid userId);
    Task<IEnumerable<StaffInvitation>> GetExpiredPendingInvitationsAsync();
}

