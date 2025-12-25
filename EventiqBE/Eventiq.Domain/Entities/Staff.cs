namespace Eventiq.Domain.Entities;

public class Staff : BaseEntity
{
    public Guid EventId { get; set; }
    public virtual Event Event { get; set; }
    
    public Guid UserId { get; set; }
    
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    
    public virtual ICollection<StaffTaskAssignment> TaskAssignments { get; set; } = new List<StaffTaskAssignment>();
}

