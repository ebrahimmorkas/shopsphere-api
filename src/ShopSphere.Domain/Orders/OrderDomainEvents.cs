using ShopSphere.Domain.Abstractions;
using ShopSphere.Domain.Shared;

namespace ShopSphere.Domain.Orders;

public sealed record OrderPlacedDomainEvent(Guid OrderId, Guid CustomerId, Money Total) : IDomainEvent;

public sealed record OrderShippedDomainEvent(Guid OrderId, Guid CustomerId) : IDomainEvent;

public sealed record OrderCancelledDomainEvent(Guid OrderId, IReadOnlyList<CancelledOrderLine> Lines) : IDomainEvent;

public sealed record CancelledOrderLine(Guid ProductId, int Quantity);
