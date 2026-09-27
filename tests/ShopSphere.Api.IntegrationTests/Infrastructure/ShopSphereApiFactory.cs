using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using ShopSphere.Infrastructure.Authentication;
using ShopSphere.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace ShopSphere.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real API in-memory against a throwaway PostgreSQL container, so tests exercise the full
/// HTTP pipeline, EF Core mappings, migrations and database constraints exactly as in production.
/// </summary>
public sealed class ShopSphereApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@shopsphere.test";
    public const string AdminPassword = "Admin123!";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("shopsphere")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:Database", _database.GetConnectionString());
        builder.UseSetting("ConnectionStrings:Redis", string.Empty);
        builder.UseSetting("Jwt:Secret", "integration-tests-secret-0123456789-abcdefgh");
        builder.UseSetting("Seed:Admin:Email", AdminEmail);
        builder.UseSetting("Seed:Admin:Password", AdminPassword);

        // Tests fire many requests from one IP; keep limits out of the way.
        builder.UseSetting("RateLimiting:GlobalPermitLimit", "100000");
        builder.UseSetting("RateLimiting:AuthPermitLimit", "100000");
    }

    public async ValueTask InitializeAsync()
    {
        await _database.StartAsync();

        await Services.ApplyMigrationsAsync();
        await Services.SeedAdminAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<ShopSphereApiFactory>
{
    public const string Name = "Integration";
}
