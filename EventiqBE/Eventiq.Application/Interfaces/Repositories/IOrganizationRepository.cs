using Eventiq.Application.Dtos;
using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Repositories;

public interface IOrganizationRepository:IGenericRepository<Organization>
{
    Task<IEnumerable<Organization>> GetMyOrgsAsync(Guid userId);
    Task<int> GetUserOrgCountAsync(Guid userId);
}