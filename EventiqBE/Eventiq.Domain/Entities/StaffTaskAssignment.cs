namespace Eventiq.Domain.Entities;

public class StaffTaskAssignment : BaseEntity
{
    public Guid StaffId { get; set; }
    public virtual Staff Staff { get; set; }
    
    public Guid TaskId { get; set; }
    public virtual EventTask Task { get; set; }
    
    public Guid OptionId { get; set; }
    public virtual TaskOption Option { get; set; }
}

