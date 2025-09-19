using AutoMapper;
using Eventiq.Domain;
using Eventiq.Application.Dtos;
using Eventiq.Domain.Entities;

namespace Eventiq.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CreateOrganizationDto, OrganizationDto>();
        CreateMap<UpdateOrganizationDto, OrganizationDto>();
        CreateMap<Organization, OrganizationDto>();
        CreateMap<Organization, OrganizationDto>();
        CreateMap<Event, EventDto>();
        
    }
}
