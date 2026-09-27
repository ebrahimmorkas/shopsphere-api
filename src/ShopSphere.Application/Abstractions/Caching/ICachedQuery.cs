using ShopSphere.Application.Abstractions.Messaging;

namespace ShopSphere.Application.Abstractions.Caching;

/// <summary>
/// Queries implementing this interface are transparently cached by <c>CachingDecorator</c>.
/// </summary>
public interface ICachedQuery
{
    string CacheKey { get; }

    TimeSpan? Expiration => null;
}

public interface ICachedQuery<TResponse> : IQuery<TResponse>, ICachedQuery;

/// <summary>
/// Single source of truth for cache keys, shared by cached queries and cache invalidation.
/// </summary>
public static class CacheKeys
{
    public const string Categories = "categories:all";

    public static string Product(Guid id) => $"products:{id}";
}
