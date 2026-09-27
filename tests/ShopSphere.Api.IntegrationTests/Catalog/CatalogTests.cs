using System.Net;
using System.Net.Http.Json;
using ShopSphere.Api.IntegrationTests.Infrastructure;

namespace ShopSphere.Api.IntegrationTests.Catalog;

public class CatalogTests(ShopSphereApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreateCategory_Should_Require_Authentication()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync($"{BasePath}/categories", new { name = "Anonymous" }, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCategory_Should_Be_Forbidden_For_Customers()
    {
        using var customer = await CreateCustomerClientAsync();

        var response = await customer.PostAsJsonAsync($"{BasePath}/categories", new { name = "Customer" }, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateProduct_Should_Persist_And_Be_Retrievable()
    {
        using var admin = await CreateAdminClientAsync();

        var productId = await CreateProductAsync(admin, stock: 7, price: 49.99m);
        var product = await GetProductAsync(admin, productId);

        product.StockQuantity.ShouldBe(7);
        product.Price.ShouldBe(49.99m);
        product.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateProduct_Should_Return_Conflict_For_Duplicate_Sku()
    {
        using var admin = await CreateAdminClientAsync();
        var categoryId = await CreateCategoryAsync(admin);
        var request = new { name = "Dup", description = "", sku = "DUP-SKU-001", price = 10m, currency = "USD", stockQuantity = 1, categoryId };

        await admin.PostAsJsonAsync($"{BasePath}/products", request, CancellationToken);
        var second = await admin.PostAsJsonAsync($"{BasePath}/products", request, CancellationToken);

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetProduct_Should_Return_NotFound_ProblemDetails()
    {
        using var client = CreateClient();

        var response = await client.GetAsync($"{BasePath}/products/{Guid.NewGuid()}", CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task UpdateProduct_Should_Invalidate_Cached_Product()
    {
        using var admin = await CreateAdminClientAsync();
        var productId = await CreateProductAsync(admin, price: 10m);
        var original = await GetProductAsync(admin, productId); // populates the cache
        var categoryId = (await admin.GetFromJsonAsync<CategoryIdDto>($"{BasePath}/products/{productId}", CancellationToken))!.CategoryId;

        var update = await admin.PutAsJsonAsync(
            $"{BasePath}/products/{productId}",
            new { name = "Renamed", description = "", price = 15m, currency = "USD", categoryId },
            CancellationToken);

        update.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        original.Price.ShouldBe(10m);
        (await GetProductAsync(admin, productId)).Price.ShouldBe(15m);
    }

    [Fact]
    public async Task SearchProducts_Should_Filter_By_Category_And_Price()
    {
        using var admin = await CreateAdminClientAsync();
        var categoryId = await CreateCategoryAsync(admin);
        foreach (var price in new[] { 5m, 50m, 500m })
        {
            await admin.PostAsJsonAsync(
                $"{BasePath}/products",
                new { name = $"Item {price}", description = "", sku = $"S-{Guid.NewGuid():N}"[..12], price, currency = "USD", stockQuantity = 1, categoryId },
                CancellationToken);
        }

        var page = await admin.GetFromJsonAsync<PageDto>(
            $"{BasePath}/products?categoryId={categoryId}&minPrice=10&maxPrice=100",
            CancellationToken);

        page!.TotalCount.ShouldBe(1);
        page.Items.ShouldHaveSingleItem().Price.ShouldBe(50m);
    }

    [Fact]
    public async Task SearchProducts_Should_Reject_Invalid_Paging()
    {
        using var client = CreateClient();

        var response = await client.GetAsync($"{BasePath}/products?pageSize=1000", CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private sealed record CategoryIdDto(Guid CategoryId);

    private sealed record PageDto(IReadOnlyList<ProductDto> Items, int TotalCount);
}
