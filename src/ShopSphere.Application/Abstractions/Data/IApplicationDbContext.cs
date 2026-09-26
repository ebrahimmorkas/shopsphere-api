using Microsoft.EntityFrameworkCore;
using ShopSphere.Domain.Categories;
using ShopSphere.Domain.Orders;
using ShopSphere.Domain.Products;

namespace ShopSphere.Application.Abstractions.Data;

/// <summary>
/// Abstraction over the EF Core context. <see cref="DbContext"/> already implements the
/// Unit of Work and Repository patterns, so handlers use it directly instead of thin wrappers.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Category> Categories { get; }

    DbSet<Product> Products { get; }

    DbSet<Order> Orders { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
