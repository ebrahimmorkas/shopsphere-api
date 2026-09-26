using ShopSphere.Domain.Abstractions;
using ShopSphere.Domain.Shared;

namespace ShopSphere.Domain.Products;

public sealed record ProductCreatedDomainEvent(Guid ProductId) : IDomainEvent;

public sealed record ProductPriceChangedDomainEvent(Guid ProductId, Money OldPrice, Money NewPrice) : IDomainEvent;

public sealed record ProductOutOfStockDomainEvent(Guid ProductId) : IDomainEvent;
