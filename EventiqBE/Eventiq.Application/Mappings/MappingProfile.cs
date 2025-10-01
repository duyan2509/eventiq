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
            .ForMember(dest => dest.Banner, opt => opt.MapFrom(src => src.BannerURL));
        CreateMap<EventAddressDto, EventAddress>();
        CreateMap<EventAddress, EventAddressDto>();
        CreateMap<Event, CreateEventResponse>();
        CreateMap<Event, PaymentInformationResponse>()
            .ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.Id));
        CreateMap<Event, UpdateAddressResponse>()
            .ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.Id));
        CreateMap<TicketClass, TicketClassDto>();
        CreateMap<CreateTicketClassDto, TicketClass>();
        CreateMap<CreateEventItemDto, EventItem>();
        CreateMap<UpdateEventItemDto, EventItem>();
        CreateMap<EventItem, EventItemDto>();
        CreateMap<CreateEventItemDto, EventItem>();
        CreateMap<Event, EventPreview>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<Event, EventDetail>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
        CreateMap<Chart, ChartDto>();
        CreateMap<CreateChartDto, Chart>();
    }
}
