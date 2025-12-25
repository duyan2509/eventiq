using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eventiq.Infrastructure.Persistence;

public class StaffInvitationRepository : GenericRepository<StaffInvitation>, IStaffInvitationRepository
{
    public StaffInvitationRepository(ApplicationDbContext context, ILogger<StaffInvitation> logger) : base(context, logger)
    {
    }

    public async Task<IEnumerable<StaffInvitation>> GetByEventIdAsync(Guid eventId)
    {
        return await _dbSet
            .Include(si => si.Event)
            .Include(si => si.Organization)
            .Where(si => si.EventId == eventId)
            .ToListAsync();
    }

    public async Task<IEnumerable<StaffInvitation>> GetByInvitedUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Include(si => si.Event)
            .Include(si => si.Organization)
            .Where(si => si.InvitedUserId == userId)
            .ToListAsync();
    }

    public async Task<StaffInvitation?> GetPendingByEventIdAndUserIdAsync(Guid eventId, Guid userId)
    {
        return await _dbSet
            .Include(si => si.Event)
            .Include(si => si.Organization)
            .FirstOrDefaultAsync(si => si.EventId == eventId && 
                                       si.InvitedUserId == userId && 
                                       si.Status == InvitationStatus.Pending);
    }

    public async Task<IEnumerable<StaffInvitation>> GetExpiredPendingInvitationsAsync()
    {
        return await _dbSet
            .Where(si => si.Status == InvitationStatus.Pending && 
                        si.InviteExpiredAt < DateTime.UtcNow)
            .ToListAsync();
    }
}

