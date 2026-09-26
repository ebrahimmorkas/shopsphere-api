using ShopSphere.Domain.Abstractions;

namespace ShopSphere.Domain.Products;

public static class ProductErrors
{
    public static readonly Error NameRequired =
        Error.Validation("Product.NameRequired", "Product name is required.");

    public static readonly Error NegativeStock =
        Error.Validation("Product.NegativeStock", "Stock quantity cannot be negative.");

    public static readonly Error InvalidQuantity =
        Error.Validation("Product.InvalidQuantity", "Quantity must be greater than zero.");

    public static readonly Error InvalidSku =
        Error.Validation("Product.InvalidSku", "SKU must be 4-32 characters of letters, digits or dashes.");

    public static readonly Error SkuNotUnique =
        Error.Conflict("Product.SkuNotUnique", "A product with the same SKU already exists.");

    public static Error NotFound(Guid id) =>
        Error.NotFound("Product.NotFound", $"The product with id '{id}' was not found.");

    public static Error Inactive(Guid id) =>
        Error.Conflict("Product.Inactive", $"The product with id '{id}' is not available for sale.");

    public static Error InsufficientStock(Guid id, int available, int requested) =>
        Error.Conflict(
            "Product.InsufficientStock",
            $"Product '{id}' has only {available} item(s) in stock but {requested} were requested.");
}
