namespace ShopSphere.Domain.Orders;

public enum OrderStatus
{
    Pending = 0,
    Placed = 1,
    Paid = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}
