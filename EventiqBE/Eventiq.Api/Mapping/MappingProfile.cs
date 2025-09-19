using AutoMapper;
using Eventiq.Api.Request;
using Eventiq.Application.Dtos;
using Eventiq.Domain.Entities;

namespace Eventiq.Api.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CreateOrganizationRequest, CreateOrganizationDto>();
        CreateMap<UpdateOrganizationRequest, UpdateOrganizationDto>();
        CreateMap<CreateEventRequest, CreateEventDto>();
        CreateMap<UpdateEventRequest, UpdateEventDto>();
    }
}
