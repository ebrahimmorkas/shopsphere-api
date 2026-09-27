using Microsoft.EntityFrameworkCore;
using ShopSphere.Application.Abstractions.Authentication;
using ShopSphere.Application.Abstractions.Data;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Domain.Abstractions;
using ShopSphere.Domain.Orders;

namespace ShopSphere.Application.Orders.GetById;

public sealed record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderResponse>;

internal sealed class GetOrderByIdQueryHandler(IApplicationDbContext dbContext, IUserContext userContext)
    : IQueryHandler<GetOrderByIdQuery, OrderResponse>
{
    public async Task<Result<OrderResponse>> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .VisibleTo(userContext)
            .Where(o => o.Id == query.OrderId)
            .Select(OrderResponse.Projection)
            .SingleOrDefaultAsync(cancellationToken);

        return order is null
            ? Result.Failure<OrderResponse>(OrderErrors.NotFound(query.OrderId))
            : order;
    }
}
