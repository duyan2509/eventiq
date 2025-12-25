using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eventiq.Infrastructure.Persistence;

public class TaskOptionRepository : GenericRepository<TaskOption>, ITaskOptionRepository
{
    public TaskOptionRepository(ApplicationDbContext context, ILogger<TaskOption> logger) : base(context, logger)
    {
    }

    public async Task<IEnumerable<TaskOption>> GetByTaskIdAsync(Guid taskId)
    {
        return await _dbSet
            .Include(to => to.StaffAssignments)
                .ThenInclude(sa => sa.Staff)
            .Where(to => to.TaskId == taskId)
            .ToListAsync();
    }

    public async Task<TaskOption?> GetByIdWithAssignmentsAsync(Guid optionId)
    {
        return await _dbSet
            .Include(to => to.StaffAssignments)
                .ThenInclude(sa => sa.Staff)
            .Include(to => to.Task)
            .FirstOrDefaultAsync(to => to.Id == optionId);
    }

    public async Task<bool> HasAssignmentsAsync(Guid optionId)
    {
        return await _dbSet
            .Where(to => to.Id == optionId)
            .SelectMany(to => to.StaffAssignments)
            .AnyAsync();
    }
}

