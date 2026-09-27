using Microsoft.EntityFrameworkCore;
using ShopSphere.Application.Abstractions.Data;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Domain.Orders;

namespace ShopSphere.Application.Orders.EventHandlers;

/// <summary>
/// Returns reserved stock to the catalog when an order is cancelled.
/// Runs inside the same unit of work as the cancellation, so both succeed or fail together.
/// </summary>
internal sealed class OrderCancelledDomainEventHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    : IDomainEventHandler<OrderCancelledDomainEvent>
{
    public async Task Handle(OrderCancelledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var quantities = domainEvent.Lines
            .GroupBy(l => l.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

        var products = await dbContext.Products
            .Where(p => quantities.Keys.Contains(p.Id))
            .ToListAsync(cancellationToken);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var product in products)
        {
            product.AddStock(quantities[product.Id], utcNow);
        }
    }
}
