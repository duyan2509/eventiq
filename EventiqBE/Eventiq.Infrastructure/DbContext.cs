using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using Eventiq.Domain.Entities;
using Eventiq.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Eventiq.Infrastructure;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    private DbSet<Event> Events { get; set; }
    private DbSet<EventAddress> EventAddresses { get; set; }
    private DbSet<Organization> Organizations { get; set; }
    private DbSet<EventItem> EventItem { get; set; }
    private DbSet<Ticket> Tickets { get; set; }
    private DbSet<TicketClass> TicketClasses { get; set; }
    private DbSet<Chart> Charts { get; set; }
    private DbSet<EventSeatState> EventSeatStates { get; set; }
    private DbSet<EventSeat> EventSeats { get; set; }
    private DbSet<EventApprovalHistory> EventApprovalHistories { get; set; }
    private DbSet<Staff> Staffs { get; set; }
    private DbSet<StaffInvitation> StaffInvitations { get; set; }
    private DbSet<EventTask> EventTasks { get; set; }
    private DbSet<TaskOption> TaskOptions { get; set; }
    private DbSet<StaffTaskAssignment> StaffTaskAssignments { get; set; }
    private DbSet<Checkout> Checkouts { get; set; }
    private DbSet<Payment> Payments { get; set; }
    private DbSet<Payout> Payouts { get; set; }
    private DbSet<VerifyRequest> VerifyRequests { get; set; }
    private DbSet<Checkin> Checkins { get; set; }
    private DbSet<TransferRequest> TransferRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder
            .Entity<ApplicationUser>()
            .HasMany<Organization>(u => u.Organizations)
            .WithOne()
            .HasForeignKey(u => u.UserId);
        modelBuilder
            .Entity<ApplicationUser>()
            .HasMany<Ticket>(u => u.Tickets)
            .WithOne()
            .HasForeignKey(u => u.UserId);
        modelBuilder.Entity<Organization>().HasIndex(o => o.Name).IsUnique();
        modelBuilder.Entity<Organization>().HasIndex(o => o.Name).IsUnique();

        modelBuilder.Entity<Event>().HasIndex(o => o.Name).IsUnique();

        modelBuilder.Entity<Event>().HasIndex(o => new { o.Start, o.Status });

        modelBuilder.Entity<TicketClass>().HasIndex(tc => new { tc.Name, tc.EventId }).IsUnique();
        modelBuilder.Entity<EventItem>().HasIndex(ei => new { ei.Name, ei.EventId }).IsUnique();
        modelBuilder.Entity<Chart>().HasIndex(ei => new { ei.Name, ei.EventId }).IsUnique();
        modelBuilder
            .Entity<EventSeatState>()
            .HasIndex(x => new { x.EventItemId, x.EventSeatId })
            .IsUnique();
        modelBuilder.Entity<EventSeat>().HasIndex(x => new { x.ChartId, x.Label }).IsUnique();
        
        // Staff and Task indexes
        modelBuilder.Entity<Staff>().HasIndex(s => new { s.EventId, s.UserId }).IsUnique();
        modelBuilder.Entity<StaffInvitation>().HasIndex(si => new { si.EventId, si.InvitedUserId, si.Status })
            .HasFilter("\"Status\" = 0"); // Only unique for pending invitations
        modelBuilder.Entity<EventTask>().HasIndex(t => new { t.Name, t.EventId }).IsUnique();
        modelBuilder.Entity<TaskOption>().HasIndex(to => new { to.OptionName, to.TaskId }).IsUnique();
        modelBuilder.Entity<StaffTaskAssignment>().HasIndex(sta => new { sta.StaffId, sta.TaskId, sta.OptionId }).IsUnique();
        modelBuilder.Entity<Ticket>().HasIndex(t => t.TicketCode).IsUnique();
        modelBuilder.Entity<VerifyRequest>().HasIndex(vr => vr.TicketId);
        modelBuilder.Entity<TransferRequest>().HasIndex(tr => tr.TicketId);
        modelBuilder.Entity<TransferRequest>().HasIndex(tr => tr.ToUserId);
        
        // Global query filter for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(DbContextExtensions)
                    .GetMethod(
                        nameof(DbContextExtensions.AddIsDeletedFilter),
                        System.Reflection.BindingFlags.Static
                            | System.Reflection.BindingFlags.Public
                    )!
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(null, new object[] { modelBuilder });
            }
        }

        modelBuilder.HasDefaultSchema("identity");
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    // Ensure all DateTime properties are UTC
                    EnsureUtcDateTime(entry.Entity);
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    // Ensure all DateTime properties are UTC
                    EnsureUtcDateTime(entry.Entity);
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    private void EnsureUtcDateTime(BaseEntity entity)
    {
        // Use reflection to find all DateTime and DateTime? properties
        var properties = entity.GetType().GetProperties()
            .Where(p => p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?));

        foreach (var property in properties)
        {
            var value = property.GetValue(entity);
            if (value != null)
            {
                if (property.PropertyType == typeof(DateTime))
                {
                    var dateTime = (DateTime)value;
                    if (dateTime.Kind != DateTimeKind.Utc)
                    {
                        property.SetValue(entity, dateTime.Kind == DateTimeKind.Unspecified 
                            ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc) 
                            : dateTime.ToUniversalTime());
                    }
                }
                else if (property.PropertyType == typeof(DateTime?))
                {
                    var dateTime = (DateTime?)value;
                    if (dateTime.HasValue && dateTime.Value.Kind != DateTimeKind.Utc)
                    {
                        var utcValue = dateTime.Value.Kind == DateTimeKind.Unspecified 
                            ? DateTime.SpecifyKind(dateTime.Value, DateTimeKind.Utc) 
                            : dateTime.Value.ToUniversalTime();
                        property.SetValue(entity, utcValue);
                    }
                }
            }
        }
    }

    public static class DbContextExtensions
    {
        public static void AddIsDeletedFilter<TEntity>(ModelBuilder builder)
            where TEntity : BaseEntity
        {
            builder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
        }
    }
}
