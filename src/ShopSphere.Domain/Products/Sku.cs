using System.Text.RegularExpressions;
using ShopSphere.Domain.Abstractions;

namespace ShopSphere.Domain.Products;

public sealed partial record Sku
{
    private Sku(string value) => Value = value;

    public string Value { get; }

    public static Result<Sku> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !SkuPattern().IsMatch(value))
        {
            return ProductErrors.InvalidSku;
        }

        return new Sku(value.ToUpperInvariant());
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Za-z0-9-]{4,32}$")]
    private static partial Regex SkuPattern();
}
