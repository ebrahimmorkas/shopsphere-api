using Microsoft.EntityFrameworkCore;
using ShopSphere.Application.Abstractions.Caching;
using ShopSphere.Application.Abstractions.Data;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Domain.Abstractions;

namespace ShopSphere.Application.Categories.GetAll;

public sealed record GetCategoriesQuery : ICachedQuery<IReadOnlyList<CategoryResponse>>
{
    public string CacheKey => CacheKeys.Categories;

    public TimeSpan? Expiration => TimeSpan.FromHours(1);
}

internal sealed class GetCategoriesQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetCategoriesQuery, IReadOnlyList<CategoryResponse>>
{
    public async Task<Result<IReadOnlyList<CategoryResponse>>> Handle(
        GetCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        var categories = await dbContext.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.Slug, c.Description))
            .ToListAsync(cancellationToken);

        return categories;
    }
}
