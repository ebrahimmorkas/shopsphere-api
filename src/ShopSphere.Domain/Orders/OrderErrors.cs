using ShopSphere.Domain.Abstractions;

namespace ShopSphere.Domain.Orders;

public static class OrderErrors
{
    public static readonly Error Empty =
        Error.Validation("Order.Empty", "An order must contain at least one item.");

    public static readonly Error NotPending =
        Error.Conflict("Order.NotPending", "Items can only be changed while the order is pending.");

    public static readonly Error CurrencyMismatch =
        Error.Validation("Order.CurrencyMismatch", "All order items must use the same currency.");

    public static Error NotFound(Guid id) =>
        Error.NotFound("Order.NotFound", $"The order with id '{id}' was not found.");

    public static Error CannotCancel(OrderStatus status) =>
        Error.Conflict("Order.CannotCancel", $"An order with status '{status}' cannot be cancelled.");

    public static Error InvalidTransition(OrderStatus from, OrderStatus to) =>
        Error.Conflict("Order.InvalidTransition", $"Cannot change order status from '{from}' to '{to}'.");
}
