# Monitoring & Health Checks Architecture

**Date**: June 3, 2026  
**Scope**: Production-grade health checks and observability for UnitConverter microservices  
**References**:
- [Monitoring and Health - Cloud Native .NET](https://learn.microsoft.com/en-us/dotnet/architecture/cloud-native/monitoring-health)
- [eShop Reference Application](https://github.com/dotnet/eShop)
- [Health Checks in .NET Aspire](https://learn.microsoft.com/dotnet/aspire/fundamentals/health-checks)

---

## Executive Summary

UnitConverter implements **two-layer health monitoring**:

1. **Development**: Aspire Dashboard (local OTLP receiver) with `/health` + `/alive` endpoints
2. **Cloud**: Azure Monitor / Application Insights (via OTLP or AppInsights exporter)

Health checks assess:
- **Liveness** (`/alive`): Is the process running? (always passes for compute instances)
- **Readiness** (`/health`): Can the service handle requests? (checks dependencies: DB, Redis, downstream services)

Observability provides:
- **Logs**: Structured ILogger + OTel context
- **Metrics**: ASP.NET Core + custom (conversion counters, duration histograms)
- **Traces**: Distributed tracing across services

All **vendor-neutral** via OpenTelemetry; switchable between local Aspire and cloud backends without code changes.

---

## Section 1: Health Checks — Architecture

### 1.1 Conceptual Model

```
┌─────────────────────────────────────────────────────┐
│   Client (Load Balancer / Kubernetes / CI)          │
└──────────────────┬──────────────────────────────────┘
                   │ GET /alive (liveness probe)
                   │ GET /health (readiness probe)
                   │ (every 5-10 seconds)
                   ↓
┌─────────────────────────────────────────────────────┐
│   ASP.NET Core Service                              │
├─────────────────────────────────────────────────────┤
│ Liveness Checks (must pass immediately)             │
│  ✓ Process is running (implicit)                    │
│  ✓ Self check (tagged: "live")                      │
│                                                     │
│ Readiness Checks (can take time)                    │
│  ✓ Database connection (SQL Server / SQLite)        │
│  ✓ Redis cache (if enabled)                         │
│  ✓ Message queue (RabbitMQ / Service Bus — future)  │
│  ✓ Downstream service calls (Catalog, Auth APIs)    │
│                                                     │
│ Result: 200 OK (healthy) or 503 Unavailable         │
└─────────────────────────────────────────────────────┘
```

### 1.2 Health Check Types

| Type | Purpose | Response Time | Failure Mode |
|------|---------|---------------|--------------|
| **Liveness** | Is process alive? | < 100ms | Restart container/pod |
| **Readiness** | Can handle requests? | 100ms - 5s | Remove from load balancer |
| **Startup** | Is app ready after boot? | Up to 2min | Wait before routing |

**For UnitConverter**: Implement `/health` (readiness) + `/alive` (liveness).

### 1.3 Endpoint Design

```
GET /health → Returns 200 + { status: "Healthy", checks: [...] }
             or 503 + { status: "Unhealthy", checks: [...] }
             
GET /alive  → Returns 200 + { status: "Alive" }
             (always 200 unless process is dead)
```

**Response Format** (standardized):
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "Database",
      "status": "Healthy",
      "description": "SQL Server 2019 connection verified",
      "data": {
        "connectionTime": "45ms"
      }
    },
    {
      "name": "Redis",
      "status": "Unhealthy",
      "description": "Failed to connect: Connection refused",
      "exception": "StackExchange.Redis.RedisConnectionException"
    }
  ],
  "totalDuration": "245ms"
}
```

---

## Section 2: Implementation — Service Defaults

### 2.1 Create `UnitConverter.ServiceDefaults` Project

**File**: `src/UnitConverter.ServiceDefaults/UnitConverter.ServiceDefaults.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Core -->
    <PackageReference Include="Microsoft.Extensions.ServiceDiscovery" />
    <PackageReference Include="Microsoft.Extensions.ServiceDiscovery.Dns" />
    
    <!-- Health Checks -->
    <PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks" />
    <PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions" />
    
    <!-- OpenTelemetry -->
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" />
    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Http" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" />
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" />
    
    <!-- Resilience (already in Core) -->
    <PackageReference Include="Microsoft.Extensions.Http.Resilience" />
  </ItemGroup>

</Project>
```

### 2.2 Create Service Defaults Extension

**File**: `src/UnitConverter.ServiceDefaults/Extensions.cs`

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Reflection;

namespace UnitConverter.ServiceDefaults;

public static class Extensions
{
    // Shared ActivitySource for distributed tracing
    public static readonly ActivitySource DefaultActivitySource = new("UnitConverter");

    /// <summary>
    /// Add shared service defaults: service discovery, resilience, health checks, observability.
    /// Call this in Program.cs for HTTP APIs.
    /// </summary>
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.AddDefaultHealthChecks();
        builder.AddDefaultOpenTelemetry();
        builder.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });
        return builder;
    }

    /// <summary>
    /// Minimal defaults for worker services (no HTTP clients, no service discovery).
    /// Call this for background workers, message processors, etc.
    /// </summary>
    public static IHostApplicationBuilder AddBasicServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.AddDefaultHealthChecks();
        builder.AddDefaultOpenTelemetry();
        return builder;
    }

    /// <summary>
    /// Add default health checks: liveness + readiness.
    /// Liveness: /alive (always passes, checks process is alive)
    /// Readiness: /health (checks dependencies)
    /// </summary>
    private static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            // Liveness: simple "self" check tagged as "live"
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// Map health check endpoints in Development environment only.
    /// In production, health checks are typically managed by the platform (Kubernetes, ACA, etc.).
    /// </summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Health endpoints only in Development (security best practice)
        if (app.Environment.IsDevelopment())
        {
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = HealthCheckResponseWriter,
                AllowCachingResponses = false,
                Predicate = _ => true  // Include all checks (no tag filter)
            });

            app.MapHealthChecks("/alive", new HealthCheckOptions
            {
                ResponseWriter = HealthCheckResponseWriter,
                AllowCachingResponses = false,
                Predicate = r => r.Tags.Contains("live")  // Only liveness check
            });
        }

        // OpenTelemetry metrics (Prometheus format)
        if (app.Environment.IsDevelopment())
        {
            app.MapPrometheusScrapingEndpoint("/metrics");
        }

        return app;
    }

    /// <summary>
    /// Custom health check response writer (JSON format).
    /// Provides detailed health status for dashboards and automation.
    /// </summary>
    private static async Task HealthCheckResponseWriter(HttpContext context, HealthReport report)
    {
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration.TotalMilliseconds,
                tags = entry.Value.Tags,
                exception = entry.Value.Exception?.GetType().Name,
                data = entry.Value.Data
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(response);
    }

    /// <summary>
    /// Configure OpenTelemetry: logs, metrics, traces.
    /// Automatically exports to OTEL_EXPORTER_OTLP_ENDPOINT (set by Aspire or manually).
    /// </summary>
    private static IHostApplicationBuilder AddDefaultOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    // Add custom UnitConverter metrics
                    .AddMeter("UnitConverter");
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    // Add custom UnitConverter traces
                    .AddSource("UnitConverter");

                // Set sampling strategy
                if (builder.Environment.IsDevelopment())
                {
                    tracing.SetSampler(new AlwaysOnSampler());
                }
                else
                {
                    // Production: sample 10% of traces
                    tracing.SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(0.1)));
                }
            });

        // Conditionally add OTLP exporter if endpoint is configured
        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            builder.Services.AddOpenTelemetry()
                .UseOtlpExporter();
        }

        return builder;
    }
}
```

### 2.3 Update Program.cs in All APIs

**File**: `src/UnitConverter.Auth/Auth.Api/Program.cs` (example)

```csharp
using UnitConverter.ServiceDefaults;

var builder = WebApplicationBuilder.CreateBuilder(args);

// Add shared defaults (health checks, resilience, observability)
builder.AddServiceDefaults();

// Add application-specific services
builder.Services.AddAuthServices(builder.Configuration);

var app = builder.Build();

// Map default endpoints (health, metrics)
app.MapDefaultEndpoints();

// Map application endpoints
app.MapAuthEndpoints();

app.Run();
```

---

## Section 3: Custom Health Checks

### 3.1 Database Health Check

**File**: `src/UnitConverter.Auth/Auth.Infrastructure/HealthChecks/DatabaseHealthCheck.cs`

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using UnitConverter.Auth.Infrastructure.Data;

namespace UnitConverter.Auth.Infrastructure.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly AuthDbContext _dbContext;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(AuthDbContext dbContext, ILogger<DatabaseHealthCheck> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            // Test database connectivity
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            stopwatch.Stop();

            if (!canConnect)
            {
                _logger.LogWarning("Database health check failed: cannot connect");
                return HealthCheckResult.Unhealthy("Cannot connect to database");
            }

            _logger.LogInformation("Database health check passed in {Duration}ms", 
                stopwatch.ElapsedMilliseconds);

            return HealthCheckResult.Healthy(
                description: "Database connection verified",
                data: new { connectionTime = stopwatch.ElapsedMilliseconds });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy(
                description: $"Database error: {ex.Message}",
                exception: ex);
        }
    }
}
```

**Register in ServiceCollectionExtensions**:

```csharp
// src/UnitConverter.Auth/Auth.Infrastructure/Extensions/ServiceCollectionExtensions.cs
public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services, 
    IConfiguration config)
{
    // Register DbContext
    services.AddDbContext<AuthDbContext>(options =>
    {
        options.UseSqlServer(config.GetConnectionString("AuthDb"));
    });

    // Register health check
    services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("Database", tags: new[] { "ready" });

    return services;
}
```

### 3.2 Redis Health Check (Future)

When Redis is added:

```csharp
services.AddHealthChecks()
    .AddRedis(
        redisConnectionString: config.GetConnectionString("Redis"),
        name: "Redis",
        tags: new[] { "ready" });
```

### 3.3 Downstream Service Health Check

For Conversion Service calling Catalog Service:

```csharp
services.AddHealthChecks()
    .AddUriHealthCheck(
        uri: new Uri($"{catalogServiceUrl}/alive"),
        name: "CatalogService",
        tags: new[] { "ready" });
```

---

## Section 4: Observability — Metrics & Tracing

### 4.1 Custom Metrics (Conversion-Specific)

**File**: `src/UnitConverter.Application/Telemetry/ConversionMetrics.cs`

```csharp
using System.Diagnostics.Metrics;

namespace UnitConverter.Application.Telemetry;

public class ConversionMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _conversionsCount;
    private readonly Histogram<double> _conversionDuration;
    private readonly Counter<long> _conversionErrors;

    public ConversionMetrics()
    {
        _meter = new Meter("UnitConverter", "1.0.0");

        _conversionsCount = _meter.CreateCounter<long>(
            name: "unitconverter.conversions.count",
            unit: "1",
            description: "Total number of unit conversions performed");

        _conversionDuration = _meter.CreateHistogram<double>(
            name: "unitconverter.conversions.duration",
            unit: "ms",
            description: "Duration of unit conversion in milliseconds");

        _conversionErrors = _meter.CreateCounter<long>(
            name: "unitconverter.conversions.errors",
            unit: "1",
            description: "Total number of conversion errors");
    }

    public void RecordConversion(double durationMs, string fromUnit, string toUnit)
    {
        _conversionsCount.Add(1, new KeyValuePair<string, object?>("from_unit", fromUnit),
                                  new KeyValuePair<string, object?>("to_unit", toUnit));
        _conversionDuration.Record(durationMs);
    }

    public void RecordError(string reason)
    {
        _conversionErrors.Add(1, new KeyValuePair<string, object?>("reason", reason));
    }
}
```

**Register in DI**:

```csharp
builder.Services.AddSingleton<ConversionMetrics>();
```

**Use in handler**:

```csharp
public class ConversionHandler
{
    private readonly ConversionMetrics _metrics;

    public ConversionHandler(ConversionMetrics metrics)
    {
        _metrics = metrics;
    }

    public async Task<ConversionResult> HandleAsync(ConversionRequest request)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _conversionService.ConvertAsync(request);
            stopwatch.Stop();

            _metrics.RecordConversion(stopwatch.Elapsed.TotalMilliseconds, 
                request.FromUnit, request.ToUnit);

            return result;
        }
        catch (Exception ex)
        {
            _metrics.RecordError(ex.GetType().Name);
            throw;
        }
    }
}
```

### 4.2 Distributed Tracing

Enable across services via ActivitySource:

```csharp
using System.Diagnostics;

namespace UnitConverter.Application.Services;

public class ConversionService
{
    private static readonly ActivitySource ActivitySource = new("UnitConverter.Conversion");

    public async Task<ConversionResult> ConvertAsync(ConversionRequest request)
    {
        using var activity = ActivitySource.StartActivity("Convert");
        activity?.SetTag("from_unit", request.FromUnit);
        activity?.SetTag("to_unit", request.ToUnit);
        activity?.SetTag("value", request.Value);

        // Conversion logic
        return await ExecuteConversionAsync(request);
    }
}
```

When a Conversion Service call comes from API → Catalog Service, the trace ID flows automatically via HTTP headers (`traceparent`), creating a linked trace across services visible in dashboards.

---

## Section 5: Local Development Setup

### 5.1 Aspire AppHost

**File**: `src/UnitConverter.AppHost/Program.cs`

```csharp
using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var authApi = builder.AddProject<Projects.UnitConverter_Auth_Api>("auth-api")
    .WithHttpHealthCheck("/health")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

var catalogApi = builder.AddProject<Projects.UnitConverter_Catalog_Api>("catalog-api")
    .WithHttpHealthCheck("/health")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WaitFor(authApi);

var conversionApi = builder.AddProject<Projects.UnitConverter_Conversion_Api>("conversion-api")
    .WithHttpHealthCheck("/health")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WaitFor(catalogApi);

builder.AddProject<Projects.UnitConverter_Web>("web")
    .WithExternalHttpEndpoints()
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WaitFor(conversionApi);

builder.Build().Run();
```

**Run locally**:
```bash
cd src/UnitConverter.AppHost
dotnet run
```

Aspire automatically:
- ✅ Starts all projects in dependency order
- ✅ Waits for `/health` on each service before starting dependents
- ✅ Injects `OTEL_EXPORTER_OTLP_ENDPOINT` to each service
- ✅ Opens dashboard at `http://localhost:19888/login?t=...`

### 5.2 Aspire Dashboard (Local OTLP Receiver)

**Automatic with AppHost** — no additional setup needed.

**Manual (without AppHost)**:
```bash
docker run --rm -p 4317:4317 -p 18888:18888 \
  mcr.microsoft.com/dotnet/aspire-dashboard:latest
```

Then set environment variable:
```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
dotnet run
```

Dashboard available at `http://localhost:18888`

### 5.3 Docker Compose (Alternative to AppHost)

If developers prefer `docker-compose`:

**File**: `docker-compose.yml`

```yaml
version: '3.8'

services:
  aspire-dashboard:
    image: mcr.microsoft.com/dotnet/aspire-dashboard:latest
    ports:
      - "18888:18888"
      - "4317:4317"  # OTLP receiver
    environment:
      - ASPNETCORE_ENVIRONMENT=Development

  auth-api:
    build:
      context: .
      dockerfile: src/UnitConverter.Auth/Auth.Api/Dockerfile
    ports:
      - "7000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - OTEL_EXPORTER_OTLP_ENDPOINT=http://aspire-dashboard:4317
      - ConnectionStrings__AuthDb=Data Source=./auth.db

  catalog-api:
    build:
      context: .
      dockerfile: src/UnitConverter.Catalog/Catalog.Api/Dockerfile
    ports:
      - "7001:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - OTEL_EXPORTER_OTLP_ENDPOINT=http://aspire-dashboard:4317
      - ConnectionStrings__CatalogDb=Data Source=./catalog.db
    depends_on:
      - auth-api

  conversion-api:
    build:
      context: .
      dockerfile: src/UnitConverter.Conversion/Conversion.Api/Dockerfile
    ports:
      - "7002:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - OTEL_EXPORTER_OTLP_ENDPOINT=http://aspire-dashboard:4317
    depends_on:
      - auth-api
      - catalog-api
```

**Run**:
```bash
docker-compose up
```

---

## Section 6: Cloud Deployment

### 6.1 Azure App Service

**Health Endpoints**: Expose `/health` and `/alive` in production (with auth if needed).

**Configuration** (environment variables in Azure Portal):
```
ASPNETCORE_ENVIRONMENT=Production
OTEL_EXPORTER_OTLP_ENDPOINT=https://your-monitoring-backend/v1/traces
APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=...
```

### 6.2 Application Insights (Optional)

Add in `Program.cs` for cloud environments:

```csharp
if (!app.Environment.IsDevelopment())
{
    builder.Services.AddApplicationInsightsTelemetry();
    
    // Or use OpenTelemetry exporter
    builder.AddOpenTelemetry()
        .UseAzureMonitorExporter(options =>
        {
            options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        });
}
```

### 6.3 Azure Container Instances / Kubernetes

Health probes configured in deployment manifest:

```yaml
# Kubernetes example
spec:
  containers:
  - name: auth-api
    image: unitconverter-auth:latest
    livenessProbe:
      httpGet:
        path: /alive
        port: 80
      initialDelaySeconds: 30
      periodSeconds: 10
      failureThreshold: 3
    readinessProbe:
      httpGet:
        path: /health
        port: 80
      initialDelaySeconds: 10
      periodSeconds: 5
      failureThreshold: 3
    resources:
      limits:
        memory: "512Mi"
        cpu: "500m"
      requests:
        memory: "256Mi"
        cpu: "250m"
```

---

## Section 7: Monitoring Dashboard & Alerts

### 7.1 Aspire Dashboard Views

**Traces**: Distributed tracing across API calls → Catalog → Auth  
**Logs**: Structured logs filtered by trace ID or service  
**Metrics**: Conversion counters, latency histograms, error rates  
**Resources**: CPU, memory per service; health status

### 7.2 Example Queries

**In Aspire Dashboard or Application Insights**:

```kusto
// Conversion success rate
ConversionMetrics
| where name == "unitconverter.conversions.count"
| summarize SuccessCount = sum(tolong(value)) by bin(timestamp, 5m)
| render timechart

// Error rate
ConversionMetrics
| where name == "unitconverter.conversions.errors"
| summarize ErrorCount = sum(tolong(value)) by bin(timestamp, 5m)

// Average conversion duration
ConversionMetrics
| where name == "unitconverter.conversions.duration"
| summarize AvgDuration = avg(todouble(value)) by bin(timestamp, 1m)
| render timechart
```

### 7.3 Alerts

Set up in Azure Monitor:
- **Alert**: Average conversion duration > 500ms for 5 minutes
- **Alert**: Error rate > 5% for 3 minutes
- **Alert**: Service unhealthy (health check failing) for 1 minute

---

## Section 8: Implementation Checklist

- [ ] Create `UnitConverter.ServiceDefaults` project
- [ ] Implement `Extensions.cs` (health checks + OTel)
- [ ] Add `MapDefaultEndpoints()` to each API `Program.cs`
- [ ] Add database health check to Auth/Catalog services
- [ ] Create custom `ConversionMetrics` class
- [ ] Instrument conversion handler with metrics + traces
- [ ] Set up Aspire AppHost with all services
- [ ] Test health endpoints locally: `curl http://localhost:7000/health`
- [ ] Test Aspire dashboard: `http://localhost:19888`
- [ ] Create `docker-compose.yml` for non-Aspire environments
- [ ] Configure cloud monitoring (Application Insights or Prometheus)
- [ ] Set up alert rules in Azure Monitor
- [ ] Document dashboard usage in README

---

## Summary

| Aspect | Implementation |
|--------|-----------------|
| **Health Endpoints** | `/health` (readiness), `/alive` (liveness) in Development |
| **Metrics** | ASP.NET Core + custom conversion counters via OpenTelemetry |
| **Traces** | Distributed across services via ActivitySource + auto-propagation |
| **Logs** | Structured via ILogger + OTel context |
| **Local Dev** | Aspire AppHost + built-in dashboard (http://localhost:19888) |
| **Docker** | `docker-compose.yml` with Aspire dashboard container |
| **Cloud** | OTLP export to Azure Monitor / Application Insights |
| **Vendor Lock-in** | None — OpenTelemetry is portable across backends |

---

## References

- [Monitoring and Health - Cloud Native .NET](https://learn.microsoft.com/en-us/dotnet/architecture/cloud-native/monitoring-health)
- [eShop Reference: ServiceDefaults](https://github.com/dotnet/eShop/blob/main/src/eShop.ServiceDefaults/Extensions.cs)
- [Health Checks in .NET Aspire](https://learn.microsoft.com/dotnet/aspire/fundamentals/health-checks)
- [OpenTelemetry .NET Instrumentation](https://opentelemetry.io/docs/instrumentation/net/)
- [Aspire Dashboard Documentation](https://learn.microsoft.com/dotnet/aspire/dashboard)
