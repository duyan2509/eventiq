using Eventiq.Domain.Entities;

namespace Eventiq.Application.Dtos;

public class EventDto
{
    public Guid Id { get; set; }
    public required Stream Banner { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required DateTime Start { get; set; }
    public required OrganizationDto Organization { get; set; }
    public required EventAddressDto EventAddress { get; set; }
    public int BankCode { get; set; }
    public required string AccountNumber { get; set; }
    public required string AccountName { get; set; }
    public required EventStatus Status { get; set; }
}
public class CreateEventDto
{
    public required Stream BannerStream { get; set; }
    public string BannerURL { get; set; } = string.Empty;
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Guid OrganizationId { get; set; }
    public required EventAddressDto EventAddress { get; set; }
}
public class CreateEventResponse
{
    public required string Banner { get; set; } 
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Guid OrganizationId { get; set; }
    public required EventAddressDto EventAddress { get; set; }
}
public class UpdateEventDto
{
    public Stream? BannerStream { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Guid? OrganizationId { get; set; }
}
public class UpdateEventAddressDto
{
    public string? ProvinceCode {get; set;}
    public string? CommuneCode {get; set;}
    public string? ProvinceName {get; set;}
    public string? CommuneName {get; set;}
    public string? Detail { get; set; }
}
public class EventAddressDto
{
    public required string ProvinceCode {get; set;}
    public required string CommuneCode {get; set;}
    public required string ProvinceName {get; set;}
    public required string CommuneName {get; set;}
    public required string Detail { get; set; }
}
public class UpdateAddressResponse
{
    public Guid EventId { get; set; }
    public required string ProvinceCode {get; set;}
    public required string CommuneCode {get; set;}
    public required string ProvinceName {get; set;}
    public required string CommuneName {get; set;}
    public required string Detail { get; set; }
}

public class UpdatePaymentInformation
{
    public int BankCode { get; set; }
    public required string AccountNumber { get; set; }
    public required string AccountName { get; set; }
}

public class PaymentInformationResponse
{
    public Guid EventId { get; set; }
    public int BankCode { get; set; }
    public required string AccountNumber { get; set; }
    public required string AccountName { get; set; }
}