using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;
using ShopSphere.Application.Abstractions.Caching;
using ShopSphere.Domain.Categories;
using ShopSphere.Domain.Products;

namespace ShopSphere.Infrastructure.Caching;

/// <summary>
/// Evicts cache entries for every catalog entity changed in a unit of work. Centralising invalidation
/// here means no handler (including order placement, which changes stock) can forget to do it.
/// </summary>
internal sealed class CacheInvalidationInterceptor(HybridCache cache) : SaveChangesInterceptor
{
    private readonly HashSet<string> _pendingKeys = [];

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            CollectKeys(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_pendingKeys.Count > 0)
        {
            await cache.RemoveAsync(_pendingKeys, cancellationToken);
            _pendingKeys.Clear();
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _pendingKeys.Clear();
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void CollectKeys(DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            switch (entry.Entity)
            {
                case Product product:
                    _pendingKeys.Add(CacheKeys.Product(product.Id));
                    break;
                case Category:
                    _pendingKeys.Add(CacheKeys.Categories);
                    break;
            }
        }
    }
}
