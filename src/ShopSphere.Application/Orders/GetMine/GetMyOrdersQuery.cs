using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Application.Abstractions.Authentication;
using ShopSphere.Application.Abstractions.Data;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Application.Common;
using ShopSphere.Domain.Abstractions;

namespace ShopSphere.Application.Orders.GetMine;

public sealed record GetMyOrdersQuery(int Page = 1, int PageSize = 20) : IQuery<PagedList<OrderSummaryResponse>>;

internal sealed class GetMyOrdersQueryValidator : AbstractValidator<GetMyOrdersQuery>
{
    public GetMyOrdersQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
    }
}

internal sealed class GetMyOrdersQueryHandler(IApplicationDbContext dbContext, IUserContext userContext)
    : IQueryHandler<GetMyOrdersQuery, PagedList<OrderSummaryResponse>>
{
    public async Task<Result<PagedList<OrderSummaryResponse>>> Handle(
        GetMyOrdersQuery query,
        CancellationToken cancellationToken)
    {
        var orders = dbContext.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == userContext.UserId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Select(o => new OrderSummaryResponse(
                o.Id,
                o.Status.ToString(),
                o.Total.Amount,
                o.Total.Currency,
                o.Items.Sum(i => i.Quantity),
                o.CreatedAtUtc));

        return await PagedList<OrderSummaryResponse>.CreateAsync(orders, query.Page, query.PageSize, cancellationToken);
    }
}
