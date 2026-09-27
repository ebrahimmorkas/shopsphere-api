using ShopSphere.Application.Products.Create;
using ShopSphere.Application.Products.Search;

namespace ShopSphere.Application.UnitTests.Products;

public class ProductValidatorTests
{
    private static readonly CreateProductCommand ValidCommand =
        new("Zephyrus G16", "Gaming laptop", "ROG-G16", 1999.99m, "USD", 5, Guid.NewGuid());

    [Fact]
    public void CreateProduct_Should_Pass_For_Valid_Command()
    {
        new CreateProductCommandValidator().Validate(ValidCommand).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", 10, "Name")]
    [InlineData("Laptop", 0, "Price")]
    [InlineData("Laptop", -5, "Price")]
    public void CreateProduct_Should_Fail_For_Invalid_Values(string name, decimal price, string expectedProperty)
    {
        var result = new CreateProductCommandValidator().Validate(ValidCommand with { Name = name, Price = price });

        result.Errors.ShouldContain(e => e.PropertyName == expectedProperty);
    }

    [Fact]
    public void SearchProducts_Should_Fail_When_MaxPrice_Is_Below_MinPrice()
    {
        var query = new SearchProductsQuery(null, null, 100, 50, false, null, null);

        new SearchProductsQueryValidator().Validate(query)
            .Errors.ShouldContain(e => e.PropertyName == nameof(SearchProductsQuery.MaxPrice));
    }

    [Theory]
    [InlineData("rating", "asc")]
    [InlineData("price", "up")]
    public void SearchProducts_Should_Reject_Unknown_Sort_Options(string sortBy, string sortOrder)
    {
        var query = new SearchProductsQuery(null, null, null, null, false, sortBy, sortOrder);

        new SearchProductsQueryValidator().Validate(query).IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void SearchProducts_Should_Reject_Invalid_Paging(int page, int pageSize)
    {
        var query = new SearchProductsQuery(null, null, null, null, false, null, null, page, pageSize);

        new SearchProductsQueryValidator().Validate(query).IsValid.ShouldBeFalse();
    }
}
