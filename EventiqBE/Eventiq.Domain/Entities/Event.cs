namespace Eventiq.Domain.Entities;

public class Event:BaseEntity
{
    public required string Banner { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required DateTime Start { get; set; } = DateTime.MinValue;
    public virtual ICollection<EventItem> EventItem { get; set; }  = new  List<EventItem>();
    public virtual ICollection<TicketClass> TicketClasses { get; set; } = new List<TicketClass>();
    public virtual ICollection<Chart> Charts { get; set; } = new List<Chart>();
    
    public virtual required Organization Organization { get; set; } 
    public Guid OrganizationId { get; set; }
    public string? BankCode { get; set; }
    public string? AccountNumber { get; set; } = "";
    public string? AccountName { get; set; } = "";
    public virtual EventAddress EventAddress { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Draft;
    
    public virtual ICollection<EventApprovalHistory> ApprovalHistory { get; set; } 
        = new List<EventApprovalHistory>();
    
    public virtual ICollection<Staff> Staffs { get; set; } = new List<Staff>();
    public virtual ICollection<StaffInvitation> StaffInvitations { get; set; } = new List<StaffInvitation>();
    public virtual ICollection<EventTask> Tasks { get; set; } = new List<EventTask>();

}

public enum EventStatus
{
    Draft,
    Pending,      // Đã submit, chờ admin duyệt
    InProgress,  // Đang xử lý tạo seat map và vé (worker đang xử lý)
    Published,     // Đã được duyệt, đang công khai
    Rejected,     // Bị từ chối
    Cancelled     // Đã hủy
}