namespace ShopSphere.Application.Categories;

public sealed record CategoryResponse(Guid Id, string Name, string Slug, string? Description);
