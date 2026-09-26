using ShopSphere.Domain.Abstractions;

namespace ShopSphere.Domain.Categories;

public static class CategoryErrors
{
    public static readonly Error NameRequired =
        Error.Validation("Category.NameRequired", "Category name is required.");

    public static readonly Error SlugNotUnique =
        Error.Conflict("Category.SlugNotUnique", "A category with the same name already exists.");

    public static readonly Error HasProducts =
        Error.Conflict("Category.HasProducts", "A category that still contains products cannot be deleted.");

    public static Error NotFound(Guid id) =>
        Error.NotFound("Category.NotFound", $"The category with id '{id}' was not found.");
}
