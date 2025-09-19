using Eventiq.Application.Dtos;

namespace Eventiq.Application.Interfaces.Services;

public interface IOrganizationService
{
    Task<CreateOrganizationResponse> CreateOrganizationAsync(Guid userId,CreateOrganizationDto dto);
    Task<OrganizationDto> UpdateOrganizationAsync(Guid userId, Guid orgId, UpdateOrganizationDto dto);
    Task<DeleteOrganizationResponse> DeleteOrganizationAsync(Guid userId, Guid id);
    Task<IEnumerable<OrganizationDto>> GetMyOrgsAsync(Guid  userId);
}