using Microsoft.Extensions.Logging;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Domain.Orders;

namespace ShopSphere.Application.Orders.EventHandlers;

internal sealed partial class OrderPlacedDomainEventHandler(ILogger<OrderPlacedDomainEventHandler> logger)
    : IDomainEventHandler<OrderPlacedDomainEvent>
{
    public Task Handle(OrderPlacedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // Integration point for notifications (email, message bus) once those are introduced.
        LogOrderPlaced(logger, domainEvent.OrderId, domainEvent.CustomerId, domainEvent.Total.Amount, domainEvent.Total.Currency);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Order {OrderId} placed by customer {CustomerId} for {Amount} {Currency}")]
    private static partial void LogOrderPlaced(ILogger logger, Guid orderId, Guid customerId, decimal amount, string currency);
}
