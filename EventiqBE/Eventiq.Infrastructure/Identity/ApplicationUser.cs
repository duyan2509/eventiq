using Eventiq.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Eventiq.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public virtual ICollection<Organization> Organizations { get; set; }
    public virtual ICollection<Ticket> Tickets { get; set; }
}

