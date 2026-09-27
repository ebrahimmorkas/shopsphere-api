using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopSphere.Application.Abstractions.Authentication;
using ShopSphere.Domain.Users;
using ShopSphere.Infrastructure.Persistence;

namespace ShopSphere.Infrastructure.Authentication;

public static class AdminSeeder
{
    /// <summary>
    /// Creates the administrator account configured under <c>Seed:Admin</c> if it does not exist yet.
    /// </summary>
    public static async Task SeedAdminAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var email = configuration["Seed:Admin:Email"];
        var password = configuration["Seed:Admin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var normalizedEmail = User.NormalizeEmail(email);

        if (await dbContext.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
        {
            return;
        }

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        dbContext.Users.Add(User.Create(
            normalizedEmail,
            "System",
            "Administrator",
            hasher.Hash(password),
            timeProvider.GetUtcNow().UtcDateTime,
            Role.Admin));

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
