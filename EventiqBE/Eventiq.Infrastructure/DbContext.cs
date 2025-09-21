using System.Net.Sockets;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>()
            .HasMany<Organization>(u => u.Organizations)
            .WithOne()
            .HasForeignKey(u => u.UserId);
        modelBuilder.Entity<ApplicationUser>()
            .HasMany<Ticket>(u => u.Tickets)
            .WithOne()
            .HasForeignKey(u => u.UserId);
        modelBuilder.Entity<Organization>()
            .HasIndex(o => o.Name)
            .IsUnique();
        modelBuilder.Entity<Organization>()
            .HasIndex(o => o.Name)
            .IsUnique();
        
        modelBuilder.Entity<Event>()
            .HasIndex(o => o.Name)
            .IsUnique();

        modelBuilder.Entity<Event>()
            .HasIndex(o =>
                new
                {
                    o.Start,
                    o.Status
                });

        modelBuilder.Entity<TicketClass>()
            .HasIndex(tc => tc.Name)
            .IsUnique();
        
        // Global query filter for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(DbContextExtensions)
                    .GetMethod(nameof(DbContextExtensions.AddIsDeletedFilter), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)!
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
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
    public static class DbContextExtensions
    {
        public static void AddIsDeletedFilter<TEntity>(ModelBuilder builder) where TEntity : BaseEntity
        {
            builder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
        }
    }

}
