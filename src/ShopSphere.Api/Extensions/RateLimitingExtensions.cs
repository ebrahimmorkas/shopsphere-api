using System.Security.Claims;
using System.Threading.RateLimiting;

namespace ShopSphere.Api.Extensions;

internal static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";

    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var globalPermitLimit = configuration.GetValue("RateLimiting:GlobalPermitLimit", 100);
        var authPermitLimit = configuration.GetValue("RateLimiting:AuthPermitLimit", 10);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }

                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { title = "Too many requests", status = StatusCodes.Status429TooManyRequests },
                    cancellationToken);
            };

            // Authenticated callers are partitioned by user id, anonymous callers by IP address.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetPartitionKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = globalPermitLimit,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            // Stricter limit on credential endpoints to slow down brute-force attempts.
            options.AddPolicy(AuthPolicy, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = authPermitLimit,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6
                    }));
        });

        return services;
    }

    private static string GetPartitionKey(HttpContext context) =>
        context.User.FindFirstValue("sub")
        ?? context.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";
}
