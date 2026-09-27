using System.Linq.Expressions;
using ShopSphere.Domain.Products;

namespace ShopSphere.Application.Products;

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string Description,
    string Sku,
    decimal Price,
    string Currency,
    int StockQuantity,
    Guid CategoryId,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc)
{
    /// <summary>
    /// Projection used by queries so that only the required columns are selected.
    /// </summary>
    public static readonly Expression<Func<Product, ProductResponse>> Projection = p => new ProductResponse(
        p.Id,
        p.Name,
        p.Description,
        p.Sku.Value,
        p.Price.Amount,
        p.Price.Currency,
        p.StockQuantity,
        p.CategoryId,
        p.IsActive,
        p.CreatedAtUtc,
        p.UpdatedAtUtc);
}
