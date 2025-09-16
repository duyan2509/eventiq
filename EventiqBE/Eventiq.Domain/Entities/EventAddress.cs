namespace Eventiq.Domain.Entities;

public class EventAddress:BaseEntity
{
    public required string ProvinceCode {get; set;}
    public required string CommuneCode {get; set;}
    public required string ProvinceName {get; set;}
    public required string CommuneName {get; set;}
    public virtual Event Event { get; set; }
}