namespace Eventiq.Domain.Entities;

public class EventTask : BaseEntity
{
    public Guid EventId { get; set; }
    public virtual Event Event { get; set; }
    
    public required string Name { get; set; }
    public string? Description { get; set; }
    
    public bool IsDefault { get; set; } = false;
    
    public virtual ICollection<TaskOption> Options { get; set; } = new List<TaskOption>();
    public virtual ICollection<StaffTaskAssignment> StaffAssignments { get; set; } = new List<StaffTaskAssignment>();
}

