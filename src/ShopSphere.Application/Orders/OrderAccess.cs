using ShopSphere.Application.Abstractions.Authentication;
using ShopSphere.Domain.Orders;

namespace ShopSphere.Application.Orders;

internal static class OrderAccess
{
    /// <summary>
    /// Customers may only see their own orders; admins may see all. Orders owned by other customers
    /// are reported as "not found" so their existence isn't leaked.
    /// </summary>
    public static IQueryable<Order> VisibleTo(this IQueryable<Order> orders, IUserContext userContext) =>
        userContext.IsAdmin ? orders : orders.Where(o => o.CustomerId == userContext.UserId);
}
