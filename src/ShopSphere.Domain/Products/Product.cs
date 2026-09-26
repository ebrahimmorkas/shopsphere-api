using ShopSphere.Domain.Abstractions;
using ShopSphere.Domain.Shared;

namespace ShopSphere.Domain.Products;

public sealed class Product : Entity, IAuditable
{
    private Product(
        Guid id,
        string name,
        string description,
        Sku sku,
        Money price,
        int stockQuantity,
        Guid categoryId,
        DateTime createdAtUtc)
        : base(id)
    {
        Name = name;
        Description = description;
        Sku = sku;
        Price = price;
        StockQuantity = stockQuantity;
        CategoryId = categoryId;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    private Product()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public Sku Sku { get; private set; } = null!;

    public Money Price { get; private set; } = null!;

    public int StockQuantity { get; private set; }

    public Guid CategoryId { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public static Result<Product> Create(
        string name,
        string description,
        Sku sku,
        Money price,
        int stockQuantity,
        Guid categoryId,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ProductErrors.NameRequired;
        }

        if (stockQuantity < 0)
        {
            return ProductErrors.NegativeStock;
        }

        var product = new Product(
            Guid.CreateVersion7(),
            name.Trim(),
            description.Trim(),
            sku,
            price,
            stockQuantity,
            categoryId,
            utcNow);

        product.Raise(new ProductCreatedDomainEvent(product.Id));

        return product;
    }

    public Result UpdateDetails(string name, string description, Guid categoryId, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ProductErrors.NameRequired;
        }

        Name = name.Trim();
        Description = description.Trim();
        CategoryId = categoryId;
        UpdatedAtUtc = utcNow;
        return Result.Success();
    }

    public void ChangePrice(Money newPrice, DateTime utcNow)
    {
        if (newPrice == Price)
        {
            return;
        }

        var oldPrice = Price;
        Price = newPrice;
        UpdatedAtUtc = utcNow;

        Raise(new ProductPriceChangedDomainEvent(Id, oldPrice, newPrice));
    }

    public Result AddStock(int quantity, DateTime utcNow)
    {
        if (quantity <= 0)
        {
            return ProductErrors.InvalidQuantity;
        }

        StockQuantity += quantity;
        UpdatedAtUtc = utcNow;
        return Result.Success();
    }

    public Result RemoveStock(int quantity, DateTime utcNow)
    {
        if (quantity <= 0)
        {
            return ProductErrors.InvalidQuantity;
        }

        if (!IsActive)
        {
            return ProductErrors.Inactive(Id);
        }

        if (StockQuantity < quantity)
        {
            return ProductErrors.InsufficientStock(Id, StockQuantity, quantity);
        }

        StockQuantity -= quantity;
        UpdatedAtUtc = utcNow;

        if (StockQuantity == 0)
        {
            Raise(new ProductOutOfStockDomainEvent(Id));
        }

        return Result.Success();
    }

    public void Deactivate(DateTime utcNow)
    {
        IsActive = false;
        UpdatedAtUtc = utcNow;
    }

    public void Activate(DateTime utcNow)
    {
        IsActive = true;
        UpdatedAtUtc = utcNow;
    }
}
