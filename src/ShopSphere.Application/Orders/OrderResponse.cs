using System.Linq.Expressions;
using ShopSphere.Domain.Orders;

namespace ShopSphere.Application.Orders;

public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string Status,
    decimal Total,
    string Currency,
    AddressResponse ShippingAddress,
    IReadOnlyList<OrderItemResponse> Items,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc)
{
    public static readonly Expression<Func<Order, OrderResponse>> Projection = o => new OrderResponse(
        o.Id,
        o.CustomerId,
        o.Status.ToString(),
        o.Total.Amount,
        o.Total.Currency,
        new AddressResponse(
            o.ShippingAddress.Street,
            o.ShippingAddress.City,
            o.ShippingAddress.State,
            o.ShippingAddress.PostalCode,
            o.ShippingAddress.Country),
        o.Items
            .Select(i => new OrderItemResponse(i.ProductId, i.ProductName, i.UnitPrice.Amount, i.Quantity))
            .ToList(),
        o.CreatedAtUtc,
        o.UpdatedAtUtc);
}

public sealed record OrderItemResponse(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);

public sealed record AddressResponse(string Street, string City, string State, string PostalCode, string Country);

public sealed record OrderSummaryResponse(Guid Id, string Status, decimal Total, string Currency, int ItemCount, DateTime CreatedAtUtc);
