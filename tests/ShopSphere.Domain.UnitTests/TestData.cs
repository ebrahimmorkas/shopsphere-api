using ShopSphere.Domain.Products;
using ShopSphere.Domain.Shared;

namespace ShopSphere.Domain.UnitTests;

internal static class TestData
{
    public static readonly DateTime UtcNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    public static readonly Address Address = new("1 Main St", "Springfield", "IL", "62701", "US");

    public static Money Usd(decimal amount) => Money.Create(amount).Value;

    public static Product CreateProduct(decimal price = 10m, int stock = 10) =>
        Product.Create(
            "Mechanical Keyboard",
            "Hot-swappable 75% keyboard",
            Sku.Create("KB-75-BLK").Value,
            Usd(price),
            stock,
            Guid.CreateVersion7(),
            UtcNow).Value;
}
