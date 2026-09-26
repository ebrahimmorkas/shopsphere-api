using ShopSphere.Domain.Products;

namespace ShopSphere.Domain.UnitTests.Products;

public class ProductTests
{
    [Fact]
    public void Create_Should_Raise_ProductCreatedDomainEvent()
    {
        var product = TestData.CreateProduct();

        product.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<ProductCreatedDomainEvent>()
            .ProductId.ShouldBe(product.Id);
    }

    [Fact]
    public void Create_Should_Fail_When_Stock_Is_Negative()
    {
        var result = Product.Create(
            "Mouse", string.Empty, Sku.Create("MS-01").Value, TestData.Usd(5), -1, Guid.NewGuid(), TestData.UtcNow);

        result.Error.ShouldBe(ProductErrors.NegativeStock);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("has space")]
    [InlineData("SKU_WITH_UNDERSCORE")]
    public void Sku_Should_Reject_Invalid_Values(string value)
    {
        Sku.Create(value).Error.ShouldBe(ProductErrors.InvalidSku);
    }

    [Fact]
    public void RemoveStock_Should_Fail_When_Insufficient()
    {
        var product = TestData.CreateProduct(stock: 2);

        var result = product.RemoveStock(3, TestData.UtcNow);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Product.InsufficientStock");
        product.StockQuantity.ShouldBe(2);
    }

    [Fact]
    public void RemoveStock_Should_Raise_OutOfStock_When_Stock_Reaches_Zero()
    {
        var product = TestData.CreateProduct(stock: 2);
        product.ClearDomainEvents();

        product.RemoveStock(2, TestData.UtcNow).IsSuccess.ShouldBeTrue();

        product.StockQuantity.ShouldBe(0);
        product.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ProductOutOfStockDomainEvent>();
    }

    [Fact]
    public void RemoveStock_Should_Fail_When_Product_Is_Inactive()
    {
        var product = TestData.CreateProduct();
        product.Deactivate(TestData.UtcNow);

        product.RemoveStock(1, TestData.UtcNow).Error.Code.ShouldBe("Product.Inactive");
    }

    [Fact]
    public void ChangePrice_Should_Raise_Event_Only_When_Price_Changes()
    {
        var product = TestData.CreateProduct(price: 10m);
        product.ClearDomainEvents();

        product.ChangePrice(TestData.Usd(10m), TestData.UtcNow);
        product.DomainEvents.ShouldBeEmpty();

        product.ChangePrice(TestData.Usd(12m), TestData.UtcNow);
        var @event = product.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ProductPriceChangedDomainEvent>();
        @event.OldPrice.Amount.ShouldBe(10m);
        @event.NewPrice.Amount.ShouldBe(12m);
    }
}
