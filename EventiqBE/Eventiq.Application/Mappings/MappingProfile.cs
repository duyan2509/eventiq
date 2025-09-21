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
        CreateMap<Organization, CreateOrganizationResponse>();
        CreateMap<Event, EventDto>();
        CreateMap<CreateEventDto, Event>()
            .ForMember(dest=>dest.Banner, opt=>opt.MapFrom(src=>src.BannerURL));
        CreateMap<EventAddressDto, EventAddress>();
        CreateMap<EventAddress, EventAddressDto>();
        CreateMap<Event, CreateEventResponse>();
        CreateMap<Event, PaymentInformationResponse>()
            .ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.Id));
        CreateMap<Event, UpdateAddressResponse>()
            .ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.Id));
        CreateMap<TicketClass, TicketClassDto>();
        CreateMap<CreateTicketClassDto, TicketClass>();
    }
}
