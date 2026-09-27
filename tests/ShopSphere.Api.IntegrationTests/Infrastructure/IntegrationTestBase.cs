using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ShopSphere.Api.IntegrationTests.Infrastructure;

[Collection(IntegrationTestCollection.Name)]
public abstract class IntegrationTestBase(ShopSphereApiFactory factory)
{
    protected const string BasePath = "/api/v1";

    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    protected HttpClient CreateClient() => factory.CreateClient();

    protected async Task<HttpClient> CreateAdminClientAsync() =>
        await CreateAuthenticatedClientAsync(ShopSphereApiFactory.AdminEmail, ShopSphereApiFactory.AdminPassword);

    protected async Task<HttpClient> CreateCustomerClientAsync()
    {
        var email = $"customer-{Guid.NewGuid():N}@example.com";
        const string password = "Passw0rd!";

        using var anonymous = CreateClient();
        var response = await anonymous.PostAsJsonAsync(
            $"{BasePath}/auth/register",
            new { email, firstName = "Test", lastName = "Customer", password },
            CancellationToken);
        response.EnsureSuccessStatusCode();

        return await CreateAuthenticatedClientAsync(email, password);
    }

    protected async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        var client = CreateClient();
        var tokens = await LoginAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return client;
    }

    protected static async Task<TokenDto> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync($"{BasePath}/auth/login", new { email, password }, CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TokenDto>(CancellationToken))!;
    }

    protected static async Task<Guid> CreateCategoryAsync(HttpClient admin)
    {
        var response = await admin.PostAsJsonAsync(
            $"{BasePath}/categories",
            new { name = $"Category {Guid.NewGuid():N}" },
            CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IdDto>(CancellationToken))!.Id;
    }

    protected static async Task<Guid> CreateProductAsync(HttpClient admin, int stock = 10, decimal price = 25m)
    {
        var categoryId = await CreateCategoryAsync(admin);
        var response = await admin.PostAsJsonAsync(
            $"{BasePath}/products",
            new
            {
                name = "Test Product",
                description = "Created by integration tests",
                sku = $"SKU-{Guid.NewGuid():N}"[..16],
                price,
                currency = "USD",
                stockQuantity = stock,
                categoryId
            },
            CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IdDto>(CancellationToken))!.Id;
    }

    protected static async Task<ProductDto> GetProductAsync(HttpClient client, Guid productId) =>
        (await client.GetFromJsonAsync<ProductDto>($"{BasePath}/products/{productId}", CancellationToken))!;

    protected static object OrderRequest(Guid productId, int quantity) => new
    {
        shippingAddress = new { street = "1 Main St", city = "Austin", state = "TX", postalCode = "73301", country = "US" },
        items = new[] { new { productId, quantity } }
    };

    protected sealed record IdDto(Guid Id);

    protected sealed record TokenDto(string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshToken);

    protected sealed record ProductDto(Guid Id, string Name, decimal Price, int StockQuantity, bool IsActive);
}
