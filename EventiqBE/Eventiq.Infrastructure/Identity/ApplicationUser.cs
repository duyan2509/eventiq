using Eventiq.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Eventiq.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public virtual ICollection<Organization> Organizations { get; set; }
    public virtual ICollection<Ticket> Tickets { get; set; }
    public bool IsBanned { get; set; } = false;
    public DateTime? BannedAt { get; set; }
    public string? BanReason { get; set; }
    public string? BannedByUserId { get; set; }
    public DateTime? UnbannedAt { get; set; }
    public string? UnbannedByUserId { get; set; }
}

