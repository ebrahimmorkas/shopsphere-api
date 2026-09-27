using Microsoft.EntityFrameworkCore;
using ShopSphere.Application.Abstractions.Data;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Domain.Abstractions;
using ShopSphere.Domain.Products;

namespace ShopSphere.Application.Products.GetById;

public sealed record GetProductByIdQuery(Guid ProductId) : IQuery<ProductResponse>;

internal sealed class GetProductByIdQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetProductByIdQuery, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == query.ProductId)
            .Select(ProductResponse.Projection)
            .SingleOrDefaultAsync(cancellationToken);

        return product is null
            ? Result.Failure<ProductResponse>(ProductErrors.NotFound(query.ProductId))
            : product;
    }
}
