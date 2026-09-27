using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ShopSphere.Application.Abstractions.Authentication;
using ShopSphere.Application.Abstractions.Data;
using ShopSphere.Domain.Users;
using ShopSphere.Infrastructure.Authentication;
using ShopSphere.Infrastructure.Caching;
using ShopSphere.Infrastructure.DomainEvents;
using ShopSphere.Infrastructure.Persistence;

namespace ShopSphere.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddCaching(configuration);
        services.AddPersistence(configuration);
        services.AddAuthenticationInternal(configuration);
        services.AddHealthChecksInternal(configuration);

        return services;
    }

    private static IServiceCollection AddHealthChecksInternal(this IServiceCollection services, IConfiguration configuration)
    {
        var healthChecks = services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("postgres", tags: ["ready"]);

        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            healthChecks.AddRedis(redisConnectionString, "redis", tags: ["ready"]);
        }

        return services;
    }

    private static IServiceCollection AddAuthenticationInternal(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtRegisteredClaimNames.Name,
                    RoleClaimType = CustomClaims.Role
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.Admin, policy => policy.RequireRole(nameof(Role.Admin)));

        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenProvider, TokenProvider>();

        return services;
    }

    private static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        // Redis becomes the distributed L2 cache when configured; otherwise HybridCache runs in-memory only.
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "shopsphere:";
            });
        }

        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            };
        });

        services.AddScoped<CacheInvalidationInterceptor>();

        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'Database' is not configured.");
        }

        services.AddDbContext<ApplicationDbContext>((sp, options) => options
            .UseNpgsql(connectionString, npgsql => npgsql
                .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Default)
                .EnableRetryOnFailure(maxRetryCount: 3))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<CacheInvalidationInterceptor>()));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IDomainEventsDispatcher, DomainEventsDispatcher>();

        return services;
    }
}
