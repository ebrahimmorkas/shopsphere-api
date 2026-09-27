using System.Net;
using System.Net.Http.Json;
using ShopSphere.Api.IntegrationTests.Infrastructure;

namespace ShopSphere.Api.IntegrationTests.Orders;

public class OrderTests(ShopSphereApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task PlaceOrder_Should_Reserve_Stock_And_Calculate_Total()
    {
        using var admin = await CreateAdminClientAsync();
        using var customer = await CreateCustomerClientAsync();
        var productId = await CreateProductAsync(admin, stock: 5, price: 20m);

        var response = await customer.PostAsJsonAsync($"{BasePath}/orders", OrderRequest(productId, 2), CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var orderId = (await response.Content.ReadFromJsonAsync<IdDto>(CancellationToken))!.Id;
        var order = await customer.GetFromJsonAsync<OrderDto>($"{BasePath}/orders/{orderId}", CancellationToken);
        order!.Status.ShouldBe("Placed");
        order.Total.ShouldBe(40m);
        (await GetProductAsync(admin, productId)).StockQuantity.ShouldBe(3);
    }

    [Fact]
    public async Task PlaceOrder_Should_Return_Conflict_When_Stock_Is_Insufficient()
    {
        using var admin = await CreateAdminClientAsync();
        using var customer = await CreateCustomerClientAsync();
        var productId = await CreateProductAsync(admin, stock: 1);

        var response = await customer.PostAsJsonAsync($"{BasePath}/orders", OrderRequest(productId, 2), CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await GetProductAsync(admin, productId)).StockQuantity.ShouldBe(1);
    }

    [Fact]
    public async Task GetOrder_Should_Return_NotFound_For_Another_Customers_Order()
    {
        using var admin = await CreateAdminClientAsync();
        using var owner = await CreateCustomerClientAsync();
        using var stranger = await CreateCustomerClientAsync();
        var productId = await CreateProductAsync(admin);
        var created = await owner.PostAsJsonAsync($"{BasePath}/orders", OrderRequest(productId, 1), CancellationToken);
        var orderId = (await created.Content.ReadFromJsonAsync<IdDto>(CancellationToken))!.Id;

        (await stranger.GetAsync($"{BasePath}/orders/{orderId}", CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await admin.GetAsync($"{BasePath}/orders/{orderId}", CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CancelOrder_Should_Restock_Products()
    {
        using var admin = await CreateAdminClientAsync();
        using var customer = await CreateCustomerClientAsync();
        var productId = await CreateProductAsync(admin, stock: 4);
        var created = await customer.PostAsJsonAsync($"{BasePath}/orders", OrderRequest(productId, 3), CancellationToken);
        var orderId = (await created.Content.ReadFromJsonAsync<IdDto>(CancellationToken))!.Id;

        var cancel = await customer.PostAsync($"{BasePath}/orders/{orderId}/cancel", null, CancellationToken);

        cancel.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await GetProductAsync(admin, productId)).StockQuantity.ShouldBe(4);
    }

    [Fact]
    public async Task Order_Lifecycle_Should_Be_Enforced_And_Admin_Only()
    {
        using var admin = await CreateAdminClientAsync();
        using var customer = await CreateCustomerClientAsync();
        var productId = await CreateProductAsync(admin);
        var created = await customer.PostAsJsonAsync($"{BasePath}/orders", OrderRequest(productId, 1), CancellationToken);
        var orderId = (await created.Content.ReadFromJsonAsync<IdDto>(CancellationToken))!.Id;

        (await customer.PostAsync($"{BasePath}/orders/{orderId}/pay", null, CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await admin.PostAsync($"{BasePath}/orders/{orderId}/ship", null, CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await admin.PostAsync($"{BasePath}/orders/{orderId}/pay", null, CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await admin.PostAsync($"{BasePath}/orders/{orderId}/ship", null, CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await customer.PostAsync($"{BasePath}/orders/{orderId}/cancel", null, CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Concurrent_Orders_Should_Never_Oversell_Stock()
    {
        const int stock = 3;
        const int buyers = 8;

        using var admin = await CreateAdminClientAsync();
        var productId = await CreateProductAsync(admin, stock: stock);
        var customers = await Task.WhenAll(Enumerable.Range(0, buyers).Select(_ => CreateCustomerClientAsync()));

        var responses = await Task.WhenAll(customers.Select(c =>
            c.PostAsJsonAsync($"{BasePath}/orders", OrderRequest(productId, 1), CancellationToken)));

        var succeeded = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var remaining = (await GetProductAsync(admin, productId)).StockQuantity;

        responses.ShouldAllBe(r => r.StatusCode == HttpStatusCode.Created || r.StatusCode == HttpStatusCode.Conflict);
        succeeded.ShouldBeLessThanOrEqualTo(stock);
        remaining.ShouldBe(stock - succeeded);

        foreach (var customer in customers)
        {
            customer.Dispose();
        }
    }

    private sealed record OrderDto(Guid Id, string Status, decimal Total);
}
