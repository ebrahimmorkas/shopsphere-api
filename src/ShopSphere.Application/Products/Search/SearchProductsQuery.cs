using System.Linq.Expressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Application.Abstractions.Data;
using ShopSphere.Application.Abstractions.Messaging;
using ShopSphere.Application.Common;
using ShopSphere.Domain.Abstractions;
using ShopSphere.Domain.Products;

namespace ShopSphere.Application.Products.Search;

public sealed record SearchProductsQuery(
    string? SearchTerm,
    Guid? CategoryId,
    decimal? MinPrice,
    decimal? MaxPrice,
    bool IncludeInactive,
    string? SortBy,
    string? SortOrder,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedList<ProductResponse>>;

internal sealed class SearchProductsQueryValidator : AbstractValidator<SearchProductsQuery>
{
    private static readonly string[] SortColumns = ["name", "price", "created"];

    public SearchProductsQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
        RuleFor(q => q.SearchTerm).MaximumLength(100);
        RuleFor(q => q.MinPrice).GreaterThanOrEqualTo(0).When(q => q.MinPrice.HasValue);
        RuleFor(q => q.MaxPrice)
            .GreaterThanOrEqualTo(q => q.MinPrice)
            .When(q => q.MinPrice.HasValue && q.MaxPrice.HasValue);
        RuleFor(q => q.SortBy)
            .Must(s => SortColumns.Contains(s!.ToLowerInvariant()))
            .When(q => !string.IsNullOrEmpty(q.SortBy))
            .WithMessage($"SortBy must be one of: {string.Join(", ", SortColumns)}.");
        RuleFor(q => q.SortOrder)
            .Must(s => s!.Equals("asc", StringComparison.OrdinalIgnoreCase) || s.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .When(q => !string.IsNullOrEmpty(q.SortOrder))
            .WithMessage("SortOrder must be 'asc' or 'desc'.");
    }
}

internal sealed class SearchProductsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<SearchProductsQuery, PagedList<ProductResponse>>
{
    public async Task<Result<PagedList<ProductResponse>>> Handle(
        SearchProductsQuery query,
        CancellationToken cancellationToken)
    {
        var products = dbContext.Products.AsNoTracking();

        if (!query.IncludeInactive)
        {
            products = products.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            // Provider-agnostic case-insensitive match keeps the Application layer free of Npgsql-specific APIs.
            var pattern = $"%{query.SearchTerm.Trim().ToLowerInvariant()}%";
            products = products.Where(p =>
                EF.Functions.Like(p.Name.ToLower(), pattern) ||
                EF.Functions.Like(p.Description.ToLower(), pattern));
        }

        if (query.CategoryId.HasValue)
        {
            products = products.Where(p => p.CategoryId == query.CategoryId);
        }

        if (query.MinPrice.HasValue)
        {
            products = products.Where(p => p.Price.Amount >= query.MinPrice);
        }

        if (query.MaxPrice.HasValue)
        {
            products = products.Where(p => p.Price.Amount <= query.MaxPrice);
        }

        var descending = string.Equals(query.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        var keySelector = GetSortKey(query.SortBy);

        products = descending
            ? products.OrderByDescending(keySelector).ThenBy(p => p.Id)
            : products.OrderBy(keySelector).ThenBy(p => p.Id);

        return await PagedList<ProductResponse>.CreateAsync(
            products.Select(ProductResponse.Projection),
            query.Page,
            query.PageSize,
            cancellationToken);
    }

    private static Expression<Func<Product, object>> GetSortKey(string? sortBy) => sortBy?.ToLowerInvariant() switch
    {
        "price" => p => p.Price.Amount,
        "created" => p => p.CreatedAtUtc,
        _ => p => p.Name
    };
}
