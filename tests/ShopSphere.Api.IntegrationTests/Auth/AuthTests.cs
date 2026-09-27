using System.Net;
using System.Net.Http.Json;
using ShopSphere.Api.IntegrationTests.Infrastructure;

namespace ShopSphere.Api.IntegrationTests.Auth;

public class AuthTests(ShopSphereApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Register_Then_Login_Should_Return_Tokens_And_Profile()
    {
        using var client = await CreateCustomerClientAsync();

        var response = await client.GetAsync($"{BasePath}/auth/me", CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<ProfileDto>(CancellationToken);
        profile!.Role.ShouldBe("Customer");
    }

    [Fact]
    public async Task Register_Should_Return_Conflict_For_Duplicate_Email()
    {
        using var client = CreateClient();
        var request = new { email = $"dup-{Guid.NewGuid():N}@example.com", firstName = "A", lastName = "B", password = "Passw0rd!" };

        (await client.PostAsJsonAsync($"{BasePath}/auth/register", request, CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Created);
        (await client.PostAsJsonAsync($"{BasePath}/auth/register", request, CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_Should_Return_ValidationProblem_For_Weak_Password()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            $"{BasePath}/auth/register",
            new { email = "weak@example.com", firstName = "A", lastName = "B", password = "weak" },
            CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(CancellationToken)).ShouldContain("Password");
    }

    [Fact]
    public async Task Login_Should_Return_Unauthorized_For_Wrong_Password()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            $"{BasePath}/auth/login",
            new { email = ShopSphereApiFactory.AdminEmail, password = "WrongPassw0rd!" },
            CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_Should_Return_Unauthorized_Without_Token()
    {
        using var client = CreateClient();

        (await client.GetAsync($"{BasePath}/auth/me", CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Should_Rotate_Token_And_Reject_Reuse()
    {
        using var client = CreateClient();
        var tokens = await LoginAsync(client, ShopSphereApiFactory.AdminEmail, ShopSphereApiFactory.AdminPassword);

        var first = await client.PostAsJsonAsync($"{BasePath}/auth/refresh", new { tokens.RefreshToken }, CancellationToken);
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var rotated = await first.Content.ReadFromJsonAsync<TokenDto>(CancellationToken);
        rotated!.RefreshToken.ShouldNotBe(tokens.RefreshToken);

        var reuse = await client.PostAsJsonAsync($"{BasePath}/auth/refresh", new { tokens.RefreshToken }, CancellationToken);
        reuse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // Reuse detection revokes the whole token family, including the freshly rotated token.
        var afterTheft = await client.PostAsJsonAsync($"{BasePath}/auth/refresh", new { rotated.RefreshToken }, CancellationToken);
        afterTheft.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private sealed record ProfileDto(Guid Id, string Email, string Role);
}
