using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eventiq.Infrastructure.Persistence;

public class StaffTaskAssignmentRepository : GenericRepository<StaffTaskAssignment>, IStaffTaskAssignmentRepository
{
    public StaffTaskAssignmentRepository(ApplicationDbContext context, ILogger<StaffTaskAssignment> logger) : base(context, logger)
    {
    }

    public async Task<IEnumerable<StaffTaskAssignment>> GetByStaffIdAsync(Guid staffId)
    {
        return await _dbSet
            .Include(sta => sta.Task)
            .Include(sta => sta.Staff)
            .Where(sta => sta.StaffId == staffId)
            .ToListAsync();
    }

    public async Task<IEnumerable<StaffTaskAssignment>> GetByTaskIdAsync(Guid taskId)
    {
        return await _dbSet
            .Include(sta => sta.Staff)
            .Include(sta => sta.Task)
            .Where(sta => sta.TaskId == taskId)
            .ToListAsync();
    }

    public async Task<StaffTaskAssignment?> GetByStaffIdAndTaskIdAsync(Guid staffId, Guid taskId)
    {
        return await _dbSet
            .Include(sta => sta.Task)
            .Include(sta => sta.Staff)
            .FirstOrDefaultAsync(sta => sta.StaffId == staffId && sta.TaskId == taskId);
    }
    
    public async Task<StaffTaskAssignment?> GetByStaffTaskAndOptionAsync(Guid staffId, Guid taskId, Guid optionId)
    {
        return await _dbSet
            .Include(sta => sta.Task)
            .Include(sta => sta.Option)
            .Include(sta => sta.Staff)
            .FirstOrDefaultAsync(sta => sta.StaffId == staffId && sta.TaskId == taskId && sta.OptionId == optionId);
    }
}

