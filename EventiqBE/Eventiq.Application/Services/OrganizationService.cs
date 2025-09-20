using System.Data;
using AutoMapper;
using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces;
using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Application.Interfaces.Services;
using Eventiq.Domain.Entities;

namespace Eventiq.Application.Services;

public class OrganizationService:IOrganizationService
{
    public OrganizationService(IMapper mapper, IIdentityService identityService, ICloudStorageService storageService, IOrganizationRepository orgRepository, IUnitOfWork unitOfWork, IEventRepository eventRepository, IUserService userService)
    {
        _mapper = mapper;
        _identityService = identityService;
        _storageService = storageService;
        _orgRepository = orgRepository;
        _unitOfWork = unitOfWork;
        _eventRepository = eventRepository;
        _userService = userService;
    }

    private readonly IMapper _mapper;
    private readonly IIdentityService _identityService ;
    private readonly ICloudStorageService _storageService;
    private readonly IOrganizationRepository _orgRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventRepository _eventRepository;
    private readonly IUserService _userService;
    public async Task<CreateOrganizationResponse> CreateOrganizationAsync(Guid userId, CreateOrganizationDto dto)
    {
        string? uploadedUrl = null;

        try
        {
            await _unitOfWork.BeginTransactionAsync();
            var org = new Organization
            {
                Name = dto.Name,
                Avatar = "PLACEHOLDER",
                UserId = userId.ToString()
            };
            await _orgRepository.AddAsync(org);
            uploadedUrl = await _storageService.UploadAsync(dto.AvatarStream, org.Id.ToString());
            if (uploadedUrl != null)
                org.Avatar = uploadedUrl;
            else 
                throw new Exception("Could not upload avatar");
            await _orgRepository.UpdateAsync(org);
            await _identityService.AssignOrgRole(userId);
            string newJwt = await _identityService.GenerateNewJwt(userId);
            await _unitOfWork.CommitAsync();
            
            var response = _mapper.Map<CreateOrganizationResponse>(org);
            response.Jwt= newJwt;
            return response;;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            if (uploadedUrl != null) 
                await _storageService.DeleteAsync(uploadedUrl);
            throw;
        }
    }

    public async Task<OrganizationDto> UpdateOrganizationAsync(Guid userId, Guid orgId, UpdateOrganizationDto dto)
    {
        string? uploadedUrl = null;

        try
        {
            var org = await _orgRepository.GetByIdAsync(orgId);
            if (org == null)
                throw new KeyNotFoundException("Organization not found");
            if(org.UserId != userId.ToString())
                throw new UnauthorizedAccessException("Only owner can update organization");
            await _unitOfWork.BeginTransactionAsync();
            if(!string.IsNullOrEmpty(dto.Name) && dto.Name != org.Name)
                org.Name = dto.Name;
            await _orgRepository.UpdateAsync(org);
            if (dto.AvatarStream!=null)
            {
                await _storageService.DeleteAsync(org.Avatar);
                uploadedUrl = await _storageService.UploadAsync(dto.AvatarStream, org.Name);
                if (uploadedUrl != null)
                    org.Avatar = uploadedUrl;
                else 
                    throw new Exception("Could not upload avatar");
            }
            await _orgRepository.UpdateAsync(org);
            await _unitOfWork.CommitAsync();
            return _mapper.Map<OrganizationDto>(org);
        }
        catch (DuplicateNameException ex)
        {
            throw new DuplicateNameException("Organization name is existed in system");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            if (uploadedUrl != null) 
                await _storageService.DeleteAsync(uploadedUrl);
            throw;
        }
    }

    public async Task<DeleteOrganizationResponse> DeleteOrganizationAsync(Guid userId, Guid id)
    {
        var org = await _orgRepository.GetByIdAsync(id);
        if (org == null)
            throw new KeyNotFoundException("Organization not found");
        if (org.UserId != userId.ToString())
            throw new UnauthorizedAccessException("Only owner can delete organization");
        var eventCount = await _eventRepository.GetOrgEventCountAsync(org.Id);
        if (eventCount > 0)
            throw new Exception("Organization delete failed because it has pending or published event");
        else
        {
            await _storageService.DeleteAsync(org.Avatar);
            await _orgRepository.HardDeleteAsync(id);
        }
        var userOrgCount = await _orgRepository.GetUserOrgCountAsync(userId);
        string? newJwt = null;
        if (userOrgCount == 0)
        {
            await _identityService.RemoveOrgRole(userId);
            newJwt = await _identityService.GenerateNewJwt(userId);
        }
        return new  DeleteOrganizationResponse
        {
            Jwt = newJwt,
            Success = true
        };
    }

    public async Task<IEnumerable<OrganizationDto>> GetMyOrgsAsync(Guid userId)
    {
        var orgs = await  _orgRepository.GetMyOrgsAsync(userId);
        return orgs.Select(org => _mapper.Map<OrganizationDto>(org));
    }
}