using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace PropFlow.Infrastructure.Observability;

public static class ObservabilitySetup
{
    public static IServiceCollection AddPropFlowObservability(
        this IServiceCollection services,
        IConfiguration config)
    {
        var otelEndpoint = config["OpenTelemetry:Endpoint"] ?? "http://localhost:4317";

        services
            .AddOpenTelemetry()
            .ConfigureResource(r => r
                .AddService("propflow-pms")
                .AddAttributes([
                    new("deployment.environment",
                        config["ASPNETCORE_ENVIRONMENT"] ?? "production")]))
            .WithTracing(t => t
                .AddAspNetCoreInstrumentation(opts => opts.RecordException = true)
                .AddHttpClientInstrumentation()
                .AddSource("MassTransit")
                .AddOtlpExporter(opts => opts.Endpoint = new Uri(otelEndpoint)))
            .WithMetrics(m => m
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter(opts => opts.Endpoint = new Uri(otelEndpoint)));

        services
            .AddHealthChecks()
            .AddNpgsql(
                config.GetConnectionString("Postgres") ?? string.Empty,
                name: "postgres",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["db", "ready"]);

        return services;
    }
}
