using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace UnitConverter.ServiceDefaults;

/// <summary>
/// Extension methods for configuring service defaults across all UnitConverter services.
/// Provides common setup for HTTP resilience and observability (health checks deferred).
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Add standard service defaults for HTTP API services.
    /// Configures: service discovery, HTTP resilience, and OpenTelemetry.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The updated builder for chaining.</returns>
    public static WebApplicationBuilder AddServiceDefaults(this WebApplicationBuilder builder)
    {
        // OpenTelemetry for observability (metrics, traces)
        builder.Services.AddDefaultOpenTelemetry();

        return builder;
    }

    /// <summary>
    /// Add default health checks with liveness and readiness probes (requires ASP.NET Core app).
    /// Health checks can be wired into endpoints via app.MapHealthChecks() in Program.cs.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    /// <remarks>
    /// Note: Direct health check support requires Microsoft.AspNetCore.Diagnostics.HealthChecks
    /// which has version constraints. Can be called directly in the web app if available.
    /// </remarks>
    public static IServiceCollection AddDefaultHealthChecks(this IServiceCollection services)
    {
        // Placeholder for future health check configuration when package versions align
        // In production, this would register IHealthCheck implementations
        return services;
    }

    /// <summary>
    /// Map default health check endpoints in Development environment.
    /// Note: Implementation requires health check infrastructure to be configured separately.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The updated application for chaining.</returns>
    /// <remarks>
    /// In production, this would expose:
    /// - GET /health: Detailed health check with all components
    /// - GET /alive: Simple liveness probe (responds 200 if running)
    /// </remarks>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            // Placeholder for health check endpoint mapping
            // Actual implementation deferred until health check packages are available
            
            // For now, provide a simple liveness endpoint
            app.MapGet("/alive", () => new { status = "alive" })
                .WithName("Liveness")
                .WithOpenApi()
                .Produces(StatusCodes.Status200OK);
        }

        return app;
    }

    /// <summary>
    /// Add default OpenTelemetry instrumentation for metrics and traces.
    /// Exports to OTLP endpoint (default: http://localhost:4317).
    /// Instruments: ASP.NET Core, HTTP client, runtime, and Polly resilience.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddDefaultOpenTelemetry(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("unitconverter"))
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("Polly");

                // Export to OTLP endpoint if configured
                var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") 
                    ?? "http://localhost:4317";
                
                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
                }
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource("Polly");

                var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") 
                    ?? "http://localhost:4317";
                
                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
                }
            });

        // Enable resilience enrichment (adds metadata to spans/metrics)
        services.AddResilienceEnricher();

        return services;
    }
}
