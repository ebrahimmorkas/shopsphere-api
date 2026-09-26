using ShopSphere.Domain.Abstractions;
using ShopSphere.Domain.Shared;

namespace ShopSphere.Domain.Orders;

public sealed class OrderItem : Entity
{
    internal OrderItem(Guid id, Guid orderId, Guid productId, string productName, Money unitPrice, int quantity)
        : base(id)
    {
        OrderId = orderId;
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    private OrderItem()
    {
    }

    public Guid OrderId { get; private set; }

    public Guid ProductId { get; private set; }

    /// <summary>
    /// Snapshot of the product name at the time the order was placed.
    /// </summary>
    public string ProductName { get; private set; } = string.Empty;

    /// <summary>
    /// Snapshot of the unit price at the time the order was placed.
    /// </summary>
    public Money UnitPrice { get; private set; } = null!;

    public int Quantity { get; private set; }

    public Money LineTotal => UnitPrice.Multiply(Quantity);

    internal void IncreaseQuantity(int quantity) => Quantity += quantity;
}
