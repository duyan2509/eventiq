using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Repositories;

public interface IStaffRepository : IGenericRepository<Staff>
{
    Task<IEnumerable<Staff>> GetByEventIdAsync(Guid eventId);
    Task<Staff?> GetByEventIdAndUserIdAsync(Guid eventId, Guid userId);
    Task<IEnumerable<Staff>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<Staff>> GetByUserIdAndMonthAsync(Guid userId, int month, int year);
    Task<Staff?> GetCurrentShiftAsync(Guid userId, DateTime currentTime);
    Task<bool> HasOverlappingScheduleAsync(Guid userId, DateTime startTime, DateTime endTime, Guid? excludeEventId = null);
}

