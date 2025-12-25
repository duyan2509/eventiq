using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Repositories;

public interface IStaffTaskAssignmentRepository : IGenericRepository<StaffTaskAssignment>
{
    Task<IEnumerable<StaffTaskAssignment>> GetByStaffIdAsync(Guid staffId);
    Task<IEnumerable<StaffTaskAssignment>> GetByTaskIdAsync(Guid taskId);
    Task<StaffTaskAssignment?> GetByStaffIdAndTaskIdAsync(Guid staffId, Guid taskId);
    Task<StaffTaskAssignment?> GetByStaffTaskAndOptionAsync(Guid staffId, Guid taskId, Guid optionId);
}

