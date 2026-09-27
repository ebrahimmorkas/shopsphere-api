using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace ShopSphere.Api.Extensions;

internal static class ObservabilityExtensions
{
    private const string ServiceName = "shopsphere-api";

    /// <summary>
    /// Configures Serilog structured logging and OpenTelemetry tracing and metrics.
    /// Telemetry is exported over OTLP when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is set
    /// (e.g. to the Aspire dashboard started by docker-compose).
    /// </summary>
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        var exportOtlp = !string.IsNullOrWhiteSpace(otlpEndpoint);

        builder.Services.AddSerilog((services, logger) =>
        {
            logger
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", ServiceName);

            if (exportOtlp)
            {
                logger.WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = otlpEndpoint;
                    options.ResourceAttributes = new Dictionary<string, object> { ["service.name"] = ServiceName };
                });
            }
        });

        var otel = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                    options.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
                .AddHttpClientInstrumentation()
                .AddNpgsql())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation());

        if (exportOtlp)
        {
            otel.UseOtlpExporter();
        }

        return builder;
    }
}
