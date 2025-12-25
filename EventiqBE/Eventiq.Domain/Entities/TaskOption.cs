namespace Eventiq.Domain.Entities;

public class TaskOption : BaseEntity
{
    public Guid TaskId { get; set; }
    public virtual EventTask Task { get; set; }
    
    public required string OptionName { get; set; } // e.g., "Gate A", "Gate B", "Gate C"
    
    public virtual ICollection<StaffTaskAssignment> StaffAssignments { get; set; } = new List<StaffTaskAssignment>();
}

