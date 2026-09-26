using System.Text.RegularExpressions;
using ShopSphere.Domain.Abstractions;

namespace ShopSphere.Domain.Categories;

public sealed partial class Category : Entity
{
    private Category(Guid id, string name, string slug, string? description)
        : base(id)
    {
        Name = name;
        Slug = slug;
        Description = description;
    }

    private Category()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public static Result<Category> Create(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return CategoryErrors.NameRequired;
        }

        var trimmed = name.Trim();
        return new Category(Guid.CreateVersion7(), trimmed, ToSlug(trimmed), description?.Trim());
    }

    public Result Update(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return CategoryErrors.NameRequired;
        }

        Name = name.Trim();
        Slug = ToSlug(Name);
        Description = description?.Trim();
        return Result.Success();
    }

    public static string ToSlug(string value) =>
        NonAlphanumeric().Replace(value.Trim().ToLowerInvariant(), "-").Trim('-');

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumeric();
}
