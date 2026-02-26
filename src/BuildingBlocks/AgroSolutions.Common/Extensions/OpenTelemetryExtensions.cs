using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace AgroSolutions.Common.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddAgroTelemetry(
        this IServiceCollection services,
        string serviceName,
        IConfiguration configuration,
        params string[] customMeterNames)
    {
        var otelEndpoint = configuration["Otel:Endpoint"] ?? "http://localhost:4317";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName, serviceVersion: "1.0.0"))
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(opts => opts.Endpoint = new Uri(otelEndpoint));

                foreach (var meterName in customMeterNames)
                    metrics.AddMeter(meterName);
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(opts => opts.Endpoint = new Uri(otelEndpoint));
            });

        return services;
    }
}
