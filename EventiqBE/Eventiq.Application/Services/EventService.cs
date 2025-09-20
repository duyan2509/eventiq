using System.Data;
using AutoMapper;
using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces;
using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Application.Interfaces.Services;
using Eventiq.Domain.Entities;

namespace Eventiq.Application.Services;

public class EventService:IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;
    private readonly ICloudStorageService _storageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventAddressRepository _eventAddressRepository;
    private readonly IOrganizationRepository  _organizationRepository;
    private readonly IIdentityService _identityService;
    public EventService(IEventRepository eventRepository, IMapper mapper, ICloudStorageService storageService, IUnitOfWork unitOfWork, IEventAddressRepository eventAddressRepository,IOrganizationRepository  organizationRepository, IIdentityService identityService)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
        _storageService = storageService;
        _unitOfWork = unitOfWork;
        _eventAddressRepository =  eventAddressRepository;
        _organizationRepository = organizationRepository;
        _identityService = identityService;
    }

    public async Task<CreateEventResponse> CreateEventInfoAsync(Guid userId, CreateEventDto dto)
    {
        string? uploadedUrl = null;
        var org = await _organizationRepository.GetByIdAsync(dto.OrganizationId);
        if (org == null)
            throw new Exception("Organization not found");
        try
        {
            await _unitOfWork.BeginTransactionAsync();
            var evnt = _mapper.Map<Event>(dto);
            await _eventRepository.AddAsync(evnt);
            uploadedUrl = await _storageService.UploadAsync(dto.BannerStream, evnt.Id.ToString());
            if (uploadedUrl != null)
            {
                evnt.Banner = uploadedUrl;
                await _eventRepository.UpdateAsync(evnt);
            }
            else
                throw new Exception("Banner upload failed");
            var address = _mapper.Map<EventAddress>(dto.EventAddress);
            address.EventId = evnt.Id;
            await _eventAddressRepository.AddAsync(address);
            await _unitOfWork.CommitAsync();
            evnt.EventAddress = address;

            return _mapper.Map<CreateEventResponse>(evnt);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            if (uploadedUrl != null) 
                await _storageService.DeleteAsync(uploadedUrl);
            throw;
        }
    }
    public async Task<EventDto> UpdateEventInfoAsync(Guid userId, Guid eventId, UpdateEventDto dto)
    {

        string? uploadedUrl = null;
        if (dto.OrganizationId != null)
        {
            var orgId = dto.OrganizationId.Value;
            var org = await _organizationRepository.GetByIdAsync(orgId);
            if (org == null)
                throw new Exception("Organization not found");            
        }
        try
        {
            await _unitOfWork.BeginTransactionAsync();
            var evnt = await _eventRepository.GetByIdAsync(eventId);
            if (evnt == null)
                throw new Exception("Event not found");
            if (dto.OrganizationId != null)
            {
                await ValidateEventOwnerAsync(userId, evnt.OrganizationId);
                await ValidateEventOwnerAsync(userId, dto.OrganizationId.Value);
                evnt.OrganizationId = dto.OrganizationId.Value;
            }
            if(dto.Name != null)
                evnt.Name = dto.Name;
            if (dto.Description != null)
                evnt.Description = dto.Description;
            await _eventRepository.UpdateAsync(evnt);
            if (dto.BannerStream != null)
            {
                await _storageService.DeleteAsync(evnt.Banner);
                uploadedUrl = await _storageService.UploadAsync(dto.BannerStream, evnt.Id.ToString());
                if (uploadedUrl != null)
                {
                    evnt.Banner = uploadedUrl;
                    await _eventRepository.UpdateAsync(evnt);
                }
                else
                    throw new Exception("Banner upload failed");
            }
            await _unitOfWork.CommitAsync();

            return _mapper.Map<EventDto>(evnt);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            if (uploadedUrl != null) 
                await _storageService.DeleteAsync(uploadedUrl);
            throw;
        }
    }
    public async Task<PaymentInformationResponse> UpdateEventPaymentAsync(Guid userId, Guid eventId, UpdatePaymentInformation dto)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new Exception("Event not found");
        await ValidateEventOwnerAsync(userId,evnt.OrganizationId);
        evnt.BankCode = dto.BankCode;
        evnt.AccountNumber = dto.AccountNumber;
        evnt.AccountName = dto.AccountName;
        await _eventRepository.UpdateAsync(evnt);
        return _mapper.Map<PaymentInformationResponse>(evnt);
    }

    public async Task<UpdateAddressResponse> UpdateEventAddressAsync(Guid userId, Guid eventId, UpdateEventAddressDto dto)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new Exception("Event not found");
        await ValidateEventOwnerAsync(userId, evnt.OrganizationId);
        var address = await _eventAddressRepository.GetByEventIdAsync(eventId);
        if(address == null)
            throw new Exception("Address not found");
        if(dto.ProvinceCode!=null)
            address.ProvinceCode = dto.ProvinceCode;
        if(dto.ProvinceName!=null)
            address.ProvinceName = dto.ProvinceName;
        if(dto.ProvinceName!=null)
            address.ProvinceName = dto.ProvinceName;
        if(dto.CommuneName!=null)
            address.CommuneName = dto.CommuneName;
        if(dto.Detail!=null)
            address.Detail = dto.Detail;
        await _eventAddressRepository.UpdateAsync(address);
        return _mapper.Map<UpdateAddressResponse>(address);
    }



    public Task<EventDto> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task ValidateEventOwnerAsync(Guid userId, Guid orgId)
    {
        var userOrgs = await _identityService.GetUserOrgsAsync(userId);
        if (!userOrgs.Contains(orgId))
            throw new UnauthorizedAccessException("User does not belong to this organization");
    }
}