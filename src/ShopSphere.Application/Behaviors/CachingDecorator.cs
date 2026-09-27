using Microsoft.Extensions.Caching.Hybrid;
using ShopSphere.Application.Abstractions.Caching;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Domain.Abstractions;

namespace ShopSphere.Application.Behaviors;

internal static class CachingDecorator
{
    internal sealed class QueryHandler<TQuery, TResponse>(
        IQueryHandler<TQuery, TResponse> innerHandler,
        HybridCache cache)
        : IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
        {
            if (query is not ICachedQuery cachedQuery)
            {
                return await innerHandler.Handle(query, cancellationToken);
            }

            Result<TResponse>? failure = null;

            try
            {
                var value = await cache.GetOrCreateAsync(
                    cachedQuery.CacheKey,
                    async token =>
                    {
                        var result = await innerHandler.Handle(query, token);
                        if (result.IsFailure)
                        {
                            // Failures (e.g. not found) must not be cached; abort the cache write.
                            failure = result;
                            throw new SkipCacheException();
                        }

                        return result.Value;
                    },
                    new HybridCacheEntryOptions { Expiration = cachedQuery.Expiration },
                    tags: null,
                    cancellationToken: cancellationToken);

                return value!;
            }
            catch (SkipCacheException) when (failure is not null)
            {
                return failure;
            }
        }
    }

    private sealed class SkipCacheException : Exception;
}
