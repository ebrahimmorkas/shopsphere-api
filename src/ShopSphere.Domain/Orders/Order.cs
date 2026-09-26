using ShopSphere.Domain.Abstractions;
using ShopSphere.Domain.Products;
using ShopSphere.Domain.Shared;

namespace ShopSphere.Domain.Orders;

public sealed class Order : Entity, IAuditable
{
    private readonly List<OrderItem> _items = [];

    private Order(Guid id, Guid customerId, Address shippingAddress, string currency, DateTime createdAtUtc)
        : base(id)
    {
        CustomerId = customerId;
        ShippingAddress = shippingAddress;
        Status = OrderStatus.Pending;
        Total = Money.Zero(currency);
        CreatedAtUtc = createdAtUtc;
    }

    private Order()
    {
    }

    public Guid CustomerId { get; private set; }

    public OrderStatus Status { get; private set; }

    public Address ShippingAddress { get; private set; } = null!;

    public Money Total { get; private set; } = null!;

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }

    public static Order Create(Guid customerId, Address shippingAddress, DateTime utcNow, string currency = Money.DefaultCurrency) =>
        new(Guid.CreateVersion7(), customerId, shippingAddress, currency, utcNow);

    /// <summary>
    /// Adds a product line to a pending order and reserves the stock on the product.
    /// </summary>
    public Result AddItem(Product product, int quantity, DateTime utcNow)
    {
        if (Status != OrderStatus.Pending)
        {
            return OrderErrors.NotPending;
        }

        if (product.Price.Currency != Total.Currency)
        {
            return OrderErrors.CurrencyMismatch;
        }

        var stockResult = product.RemoveStock(quantity, utcNow);
        if (stockResult.IsFailure)
        {
            return stockResult;
        }

        var existing = _items.SingleOrDefault(i => i.ProductId == product.Id);
        if (existing is not null)
        {
            existing.IncreaseQuantity(quantity);
        }
        else
        {
            _items.Add(new OrderItem(Guid.CreateVersion7(), Id, product.Id, product.Name, product.Price, quantity));
        }

        RecalculateTotal();
        UpdatedAtUtc = utcNow;
        return Result.Success();
    }

    public Result Place(DateTime utcNow)
    {
        if (_items.Count == 0)
        {
            return OrderErrors.Empty;
        }

        if (Status != OrderStatus.Pending)
        {
            return OrderErrors.NotPending;
        }

        Status = OrderStatus.Placed;
        UpdatedAtUtc = utcNow;
        Raise(new OrderPlacedDomainEvent(Id, CustomerId, Total));
        return Result.Success();
    }

    public Result MarkAsPaid(DateTime utcNow) => Transition(OrderStatus.Placed, OrderStatus.Paid, utcNow);

    public Result Ship(DateTime utcNow)
    {
        var result = Transition(OrderStatus.Paid, OrderStatus.Shipped, utcNow);
        if (result.IsSuccess)
        {
            Raise(new OrderShippedDomainEvent(Id, CustomerId));
        }

        return result;
    }

    public Result Deliver(DateTime utcNow) => Transition(OrderStatus.Shipped, OrderStatus.Delivered, utcNow);

    public Result Cancel(DateTime utcNow)
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Cancelled)
        {
            return OrderErrors.CannotCancel(Status);
        }

        Status = OrderStatus.Cancelled;
        CancelledAtUtc = utcNow;
        UpdatedAtUtc = utcNow;

        // Stock is released by a domain event handler so the aggregate stays independent of Product loading.
        Raise(new OrderCancelledDomainEvent(Id, _items.Select(i => new CancelledOrderLine(i.ProductId, i.Quantity)).ToList()));
        return Result.Success();
    }

    private Result Transition(OrderStatus expected, OrderStatus next, DateTime utcNow)
    {
        if (Status != expected)
        {
            return OrderErrors.InvalidTransition(Status, next);
        }

        Status = next;
        UpdatedAtUtc = utcNow;
        return Result.Success();
    }

    private void RecalculateTotal() =>
        Total = _items.Aggregate(Money.Zero(Total.Currency), (sum, item) => sum + item.LineTotal);
}
