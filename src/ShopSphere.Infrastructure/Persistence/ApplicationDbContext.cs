using Microsoft.EntityFrameworkCore;
using ShopSphere.Application.Abstractions.Data;
using ShopSphere.Domain.Abstractions;
using ShopSphere.Domain.Categories;
using ShopSphere.Domain.Orders;
using ShopSphere.Domain.Products;
using ShopSphere.Domain.Users;
using ShopSphere.Infrastructure.DomainEvents;

namespace ShopSphere.Infrastructure.Persistence;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IDomainEventsDispatcher domainEventsDispatcher)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Default);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Domain events are dispatched before committing so that side effects raised by handlers
        // (e.g. restocking products when an order is cancelled) are persisted atomically.
        await DispatchDomainEventsAsync(cancellationToken);

        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var entities = ChangeTracker.Entries<Entity>()
                .Select(e => e.Entity)
                .Where(e => e.DomainEvents.Count != 0)
                .ToList();

            if (entities.Count == 0)
            {
                return;
            }

            var domainEvents = entities.SelectMany(e => e.DomainEvents).ToList();
            entities.ForEach(e => e.ClearDomainEvents());

            await domainEventsDispatcher.DispatchAsync(domainEvents, cancellationToken);
        }
    }
}
