using ShopSphere.Domain.Orders;

namespace ShopSphere.Domain.UnitTests.Orders;

public class OrderTests
{
    private static Order CreateOrder() => Order.Create(Guid.NewGuid(), TestData.Address, TestData.UtcNow);

    [Fact]
    public void AddItem_Should_Reserve_Stock_And_Update_Total()
    {
        var order = CreateOrder();
        var product = TestData.CreateProduct(price: 25m, stock: 5);

        order.AddItem(product, 2, TestData.UtcNow).IsSuccess.ShouldBeTrue();

        product.StockQuantity.ShouldBe(3);
        order.Total.Amount.ShouldBe(50m);
        order.Items.ShouldHaveSingleItem().Quantity.ShouldBe(2);
    }

    [Fact]
    public void AddItem_Should_Merge_Lines_For_Same_Product()
    {
        var order = CreateOrder();
        var product = TestData.CreateProduct(price: 10m);

        order.AddItem(product, 1, TestData.UtcNow);
        order.AddItem(product, 2, TestData.UtcNow);

        order.Items.ShouldHaveSingleItem().Quantity.ShouldBe(3);
        order.Total.Amount.ShouldBe(30m);
    }

    [Fact]
    public void AddItem_Should_Fail_When_Stock_Is_Insufficient()
    {
        var order = CreateOrder();
        var product = TestData.CreateProduct(stock: 1);

        var result = order.AddItem(product, 2, TestData.UtcNow);

        result.Error.Code.ShouldBe("Product.InsufficientStock");
        order.Items.ShouldBeEmpty();
    }

    [Fact]
    public void Place_Should_Fail_When_Order_Is_Empty()
    {
        CreateOrder().Place(TestData.UtcNow).Error.ShouldBe(OrderErrors.Empty);
    }

    [Fact]
    public void Place_Should_Raise_OrderPlacedDomainEvent()
    {
        var order = CreateOrder();
        order.AddItem(TestData.CreateProduct(price: 15m), 2, TestData.UtcNow);

        order.Place(TestData.UtcNow).IsSuccess.ShouldBeTrue();

        order.Status.ShouldBe(OrderStatus.Placed);
        order.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<OrderPlacedDomainEvent>()
            .Total.Amount.ShouldBe(30m);
    }

    [Fact]
    public void Full_Lifecycle_Should_Follow_Valid_Transitions()
    {
        var order = CreateOrder();
        order.AddItem(TestData.CreateProduct(), 1, TestData.UtcNow);

        order.Place(TestData.UtcNow).IsSuccess.ShouldBeTrue();
        order.MarkAsPaid(TestData.UtcNow).IsSuccess.ShouldBeTrue();
        order.Ship(TestData.UtcNow).IsSuccess.ShouldBeTrue();
        order.Deliver(TestData.UtcNow).IsSuccess.ShouldBeTrue();

        order.Status.ShouldBe(OrderStatus.Delivered);
    }

    [Fact]
    public void Ship_Should_Fail_When_Order_Is_Not_Paid()
    {
        var order = CreateOrder();
        order.AddItem(TestData.CreateProduct(), 1, TestData.UtcNow);
        order.Place(TestData.UtcNow);

        order.Ship(TestData.UtcNow).Error.Code.ShouldBe("Order.InvalidTransition");
    }

    [Fact]
    public void Cancel_Should_Raise_Event_With_Lines_To_Restock()
    {
        var order = CreateOrder();
        var product = TestData.CreateProduct();
        order.AddItem(product, 3, TestData.UtcNow);
        order.Place(TestData.UtcNow);
        order.ClearDomainEvents();

        order.Cancel(TestData.UtcNow).IsSuccess.ShouldBeTrue();

        order.Status.ShouldBe(OrderStatus.Cancelled);
        var line = order.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<OrderCancelledDomainEvent>()
            .Lines.ShouldHaveSingleItem();
        line.ProductId.ShouldBe(product.Id);
        line.Quantity.ShouldBe(3);
    }

    [Fact]
    public void Cancel_Should_Fail_When_Order_Is_Shipped()
    {
        var order = CreateOrder();
        order.AddItem(TestData.CreateProduct(), 1, TestData.UtcNow);
        order.Place(TestData.UtcNow);
        order.MarkAsPaid(TestData.UtcNow);
        order.Ship(TestData.UtcNow);

        order.Cancel(TestData.UtcNow).Error.Code.ShouldBe("Order.CannotCancel");
    }
}
