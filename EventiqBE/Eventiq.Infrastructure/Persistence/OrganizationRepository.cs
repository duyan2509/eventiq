using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eventiq.Infrastructure.Persistence;

public class OrganizationRepository: GenericRepository<Organization>, IOrganizationRepository
{
    public OrganizationRepository(ApplicationDbContext context, ILogger<Organization> logger) : base(context, logger)
    {
    }

    public async Task<IEnumerable<Organization>> GetMyOrgsAsync(Guid userId)
    {
        return await  _dbSet.Where(org=>org.UserId.Equals(userId.ToString())).ToListAsync();
    }

    public async Task<int> GetUserOrgCountAsync(Guid userId)
    {
        return await _dbSet.Where(org => org.UserId.Equals(userId.ToString())).CountAsync();
    }

    public async Task<Organization?> GetByUserIdAsync(Guid userId, Guid orgId)
    {
        return await _dbSet.Where(org => org.UserId.Equals(userId.ToString()) && org.Id.Equals(orgId)).FirstOrDefaultAsync();
    }
}