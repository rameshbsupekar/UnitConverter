# Resilience Architecture: Microsoft.Extensions.Resilience & HTTP.Resilience

**Status**: Reference Architecture  
**Based On**: 
- [Building resilient cloud services with .NET 8](https://devblogs.microsoft.com/dotnet/building-resilient-cloud-services-with-dotnet-8/)
- [Introduction to resilient app development - .NET](https://learn.microsoft.com/en-us/dotnet/core/resilience/)  
**Date**: June 2026  
**Version**: 1.0

---

## Executive Summary

The UnitConverter microservices architecture implements **production-grade resilience** using Microsoft's first-party packages:

| Package | Purpose | Use Case |
|---------|---------|----------|
| **Microsoft.Extensions.Resilience** | General-purpose resilience pipelines (retry, timeout, circuit breaker, rate limiting, fallback, hedging) | Outgoing HTTP requests, inter-service calls, external dependencies |
| **Microsoft.Extensions.Http.Resilience** | HTTP-focused resilience (built on top of Resilience) | Dedicated HttpClient factory integration with standard & custom pipelines |
| **Microsoft.AspNetCore.RateLimiting** | Inbound request rate limiting (DDoS protection) | Global rate limiting middleware for incoming API requests |

**Key Principle**: Resilience is mandatory for cloud-native microservices. We use:
- **Inbound**: `Microsoft.AspNetCore.RateLimiting` middleware (global DDoS protection)
- **Outbound**: `Microsoft.Extensions.Http.Resilience` for inter-service calls & external APIs

---

## 1. Inbound Request Rate Limiting (DDoS Protection)

### 1.1 Overview
Rate limiting **must** be applied to incoming API requests to prevent DDoS attacks. ASP.NET Core 9.0+ provides native `Microsoft.AspNetCore.RateLimiting` middleware.

### 1.2 Architecture: Partitioned Rate Limiters

```csharp
// src/UnitConverter.Api/Program.cs
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplicationBuilder.CreateBuilder(args);

// Add rate limiting services
builder.Services.AddRateLimiter(options =>
{
    // Policy 1: Fixed window by IP (10 requests per second per IP)
    options.AddFixedWindowLimiter(policyName: "FixedWindow_ByIp", configure: config =>
    {
        config.PermitLimit = 10;
        config.Window = TimeSpan.FromSeconds(1);
        config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        config.QueueLimit = 5;  // Queue up to 5 requests if at limit
    })
    .Partition(key: HttpContext => 
    {
        // Partition by IP address
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    });

    // Policy 2: Sliding window by authenticated user (100 requests per minute per user)
    options.AddSlidingWindowLimiter(policyName: "SlidingWindow_ByUser", configure: config =>
    {
        config.PermitLimit = 100;
        config.Window = TimeSpan.FromMinutes(1);
        config.SegmentsPerWindow = 2;  // More granular tracking
        config.QueueLimit = 10;
    })
    .Partition(key: HttpContext =>
    {
        // Partition by authenticated user or fallback to IP
        var userId = HttpContext.User?.FindFirst("sub")?.Value ?? 
                     HttpContext.Connection.RemoteIpAddress?.ToString() ?? 
                     "anonymous";
        return userId;
    });

    // Policy 3: Token bucket by API Key (for partner APIs, 500 req/min)
    options.AddTokenBucketLimiter(policyName: "TokenBucket_ByApiKey", configure: config =>
    {
        config.TokenLimit = 500;
        config.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
        config.TokensPerPeriod = 500;
        config.QueueProcessingOrder = QueueProcessingOrder.NewestFirst;
        config.QueueLimit = 0;  // Reject immediately if over limit
    })
    .Partition(key: HttpContext =>
    {
        // Partition by API Key header
        var apiKey = HttpContext.Request.Headers["X-Api-Key"].ToString();
        return !string.IsNullOrEmpty(apiKey) ? apiKey : "no-key";
    });

    // OnRejected handler: Log and return 429 Too Many Requests
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { error = "Rate limit exceeded. Try again later." }, 
            cancellationToken: cancellationToken);
    };

    // Reject all requests (safe default fallback)
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

// Add rate limiting middleware EARLY (before auth, logging, etc.)
app.UseRateLimiter();

// Map endpoints with specific rate limit policies
app.MapGet("/api/convert", ConvertHandler)
   .RequireRateLimiting(policyName: "FixedWindow_ByIp")
   .WithName("Convert")
   .WithOpenApi();

app.MapGet("/api/units", ListUnitsHandler)
   .RequireRateLimiting(policyName: "FixedWindow_ByIp")
   .WithName("ListUnits")
   .WithOpenApi();

app.MapPost("/api/admin/units", CreateUnitHandler)
   .RequireRateLimiting(policyName: "SlidingWindow_ByUser")
   .RequireAuthorization("AdminOnly")
   .WithName("CreateUnit")
   .WithOpenApi();

app.Run();
```

### 1.3 Rate Limiting Configuration (appsettings.json)

```json
{
  "RateLimiting": {
    "Policies": {
      "FixedWindow_ByIp": {
        "PermitLimit": 10,
        "WindowSeconds": 1,
        "QueueLimit": 5
      },
      "SlidingWindow_ByUser": {
        "PermitLimit": 100,
        "WindowMinutes": 1,
        "QueueLimit": 10
      },
      "TokenBucket_ByApiKey": {
        "TokenLimit": 500,
        "ReplenishmentMinutes": 1,
        "QueueLimit": 0
      }
    }
  }
}
```

### 1.4 DDoS Protection Strategy

**Layers**:
1. **Network Layer**: Load balancer (Azure, AWS) with DDoS protection (Azure DDoS Protection Standard, AWS Shield)
2. **Application Layer**: Rate limiting middleware (by IP, user, API key)
3. **Monitoring**: Track rate limit rejections (429 responses) in Application Insights / OpenTelemetry

**Result**: Malicious actors making 100+ requests/sec from a single IP → blocked after 10 requests.

---

## 2. Outbound HTTP Resilience (Inter-Service Calls)

### 2.1 Overview
All outgoing HTTP requests to other microservices or external APIs **must** be resilient. Microsoft provides `Microsoft.Extensions.Http.Resilience` with pre-configured pipelines.

**Standard Resilience Pipeline** (recommended for most scenarios):

| Order | Strategy | Purpose |
|-------|----------|---------|
| 1 | **Rate Limiter** | Limit concurrent outgoing requests (backpressure) |
| 2 | **Total Timeout** | Fail-fast if entire request+retries exceed limit |
| 3 | **Retry** | Retry transient failures (5xx, 429, 408, timeouts) |
| 4 | **Circuit Breaker** | Pause communication if dependency is failing |
| 5 | **Attempt Timeout** | Timeout individual request attempts |

### 2.2 Setup: Add NuGet Packages

```bash
dotnet add package Microsoft.Extensions.Http.Resilience
dotnet add package Microsoft.Extensions.Resilience
```

### 2.3 Standard Resilience Handler (Recommended)

```csharp
// src/UnitConverter.Api/Program.cs
using Microsoft.Extensions.Resilience;
using Microsoft.Extensions.Http.Resilience;

var builder = WebApplicationBuilder.CreateBuilder(args);

// Setup inter-service HTTP clients with standard resilience
builder.Services
    .AddHttpClient("CatalogService", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Services:Catalog:Url"] ?? 
                                      "https://localhost:7001");
    })
    .AddStandardResilienceHandler(options =>
    {
        // Customize retry strategy
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.UseJitter = true;
        options.Retry.Delay = TimeSpan.FromMilliseconds(100);

        // Customize circuit breaker
        options.CircuitBreaker.MinimumThroughput = 10;  // Need 10+ attempts to trigger
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        options.CircuitBreaker.FailureRatio = 0.5;      // Trip if 50% fail in window

        // Customize timeouts
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);  // Includes retries
    })
    .ConfigureAdditionalHttpMessageHandlers((handler, sp) =>
    {
        // Custom logging/telemetry can be added here
    });

builder.Services
    .AddHttpClient("ConversionService", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Services:Conversion:Url"] ?? 
                                      "https://localhost:7002");
    })
    .AddStandardResilienceHandler();  // Use defaults for conversion service

// (Aspire integrations, etc.)
```

### 2.4 Custom Resilience Pipeline (Advanced)

When standard pipeline doesn't fit, build a custom one:

```csharp
// src/UnitConverter.Core/Extensions/HttpClientResilienceExtensions.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace UnitConverter.Core.Extensions;

public static class HttpClientResilienceExtensions
{
    /// <summary>
    /// Add custom resilience handler for external APIs (e.g., Google Maps, OpenWeather)
    /// Pipeline: Rate Limit -> Total Timeout -> Retry -> Circuit Breaker -> Attempt Timeout
    /// </summary>
    public static IHttpClientBuilder AddExternalApiResilienceHandler(
        this IHttpClientBuilder builder,
        string pipelineName = "ExternalApi",
        int maxRetries = 4,
        int circuitBreakerThreshold = 5)
    {
        return builder.AddResilienceHandler(
            pipelineName,
            (handlerBuilder, context) =>
            {
                // Get options from config (enable dynamic reloads)
                context.EnableReloads<ExternalApiResilienceOptions>(pipelineName);
                var options = context.GetOptions<ExternalApiResilienceOptions>(pipelineName);

                handlerBuilder
                    // 1. Rate limit concurrent requests (max 10 concurrent)
                    .AddConcurrencyLimiter(new ConcurrencyLimiterStrategyOptions
                    {
                        PermitLimit = 10,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5
                    })
                    // 2. Total request timeout (all retries must complete in 30s)
                    .AddTimeout(new HttpTimeoutStrategyOptions
                    {
                        Timeout = TimeSpan.FromSeconds(30),
                        TimeoutType = HttpTimeoutStrategyOptions.TimeoutType.TotalRequestDuration
                    })
                    // 3. Retry transient failures
                    .AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = maxRetries,
                        Delay = TimeSpan.FromMilliseconds(100),
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true,
                        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                            .Handle<HttpRequestException>()
                            .Handle<TimeoutRejectedException>()
                            .HandleResult(r => r.StatusCode >= HttpStatusCode.InternalServerError)
                            .HandleResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                            .HandleResult(r => r.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
                    })
                    // 4. Circuit breaker (stop if too many failures)
                    .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                    {
                        MinimumThroughput = 5,
                        SamplingDuration = TimeSpan.FromSeconds(60),
                        FailureRatio = 0.5,
                        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                            .Handle<HttpRequestException>()
                            .Handle<TimeoutRejectedException>()
                            .HandleResult(r => r.StatusCode >= HttpStatusCode.InternalServerError)
                    })
                    // 5. Attempt timeout (each request attempt has 3s limit)
                    .AddTimeout(new HttpTimeoutStrategyOptions
                    {
                        Timeout = TimeSpan.FromSeconds(3),
                        TimeoutType = HttpTimeoutStrategyOptions.TimeoutType.PerAttempt
                    });
            });
    }

    /// <summary>
    /// Add hedging handler for critical services (multi-endpoint routing)
    /// Hedging issues multiple concurrent requests if first one is slow
    /// </summary>
    public static IHttpClientBuilder AddHedgingResilienceHandler(
        this IHttpClientBuilder builder,
        string pipelineName = "Hedging")
    {
        return builder
            .AddStandardHedgingHandler()
            .SelectPipelineBy(serviceProvider => request => 
            {
                // Pool circuit breakers by host
                return request.RequestUri?.Host ?? "default";
            })
            .Configure(options =>
            {
                options.Hedging.Delay = TimeSpan.FromMilliseconds(500);
                options.Hedging.MaxHedgedAttempts = 2;
                options.Endpoint.CircuitBreaker.MinimumThroughput = 10;
            });
    }
}

/// <summary>
/// Configuration model for external API resilience (supports dynamic reloads)
/// </summary>
public class ExternalApiResilienceOptions
{
    public HttpRetryStrategyOptions? Retry { get; set; }
    public HttpCircuitBreakerStrategyOptions? CircuitBreaker { get; set; }
    public HttpTimeoutStrategyOptions? Timeout { get; set; }
}
```

**Usage**:
```csharp
builder.Services
    .AddHttpClient("ExternalWeatherApi", client =>
    {
        client.BaseAddress = new Uri("https://api.openweathermap.org");
        client.DefaultRequestHeaders.Add("User-Agent", "UnitConverter/1.0");
    })
    .AddExternalApiResilienceHandler(
        pipelineName: "WeatherApi",
        maxRetries: 4,
        circuitBreakerThreshold: 5);
```

### 2.5 Service-to-Service Call Pattern

```csharp
// src/UnitConverter.Api/Controllers/UnitsController.cs
[ApiController]
[Route("api/[controller]")]
public class UnitsController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UnitsController> _logger;

    public UnitsController(IHttpClientFactory httpClientFactory, ILogger<UnitsController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet("{id:guid}")]
    [ProduceResponseType(typeof(UnitDto), StatusCodes.Status200OK)]
    [ProduceResponseType(StatusCodes.Status404NotFound)]
    [ProduceResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetUnitAsync(Guid id)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("CatalogService");
            
            // Resilience is built into the HttpClient via standard handler
            var response = await httpClient.GetAsync($"/api/units/{id}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { error = "Unit not found" });
            }

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var unit = JsonSerializer.Deserialize<UnitDto>(content);

            return Ok(unit);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            _logger.LogError(ex, "Catalog service unavailable");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, 
                new { error = "Catalog service temporarily unavailable" });
        }
        catch (TimeoutRejectedException ex)
        {
            _logger.LogError(ex, "Request to Catalog service timed out");
            return StatusCode(StatusCodes.Status504GatewayTimeout, 
                new { error = "Request timeout" });
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Catalog service circuit breaker open");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, 
                new { error = "Service temporarily unavailable, try again later" });
        }
    }
}
```

### 2.6 Configuration for Dynamic Reloads

**appsettings.json**:
```json
{
  "Services": {
    "Catalog": {
      "Url": "https://localhost:7001"
    },
    "Conversion": {
      "Url": "https://localhost:7002"
    }
  },
  "Resilience": {
    "HttpClients": {
      "CatalogService": {
        "Retry": {
          "MaxRetryAttempts": 3,
          "BackoffType": 1,
          "UseJitter": true,
          "Delay": "00:00:00.1000000"
        },
        "CircuitBreaker": {
          "MinimumThroughput": 10,
          "SamplingDuration": "00:00:30",
          "FailureRatio": 0.5
        },
        "AttemptTimeout": {
          "Timeout": "00:00:03"
        },
        "TotalRequestTimeout": {
          "Timeout": "00:00:10"
        }
      }
    }
  }
}
```

---

## 3. General-Purpose Resilience Pipelines

For non-HTTP operations (database calls, message queue consumers, etc.), use `Microsoft.Extensions.Resilience`:

```csharp
// src/UnitConverter.Core/Extensions/ResilienceExtensions.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Resilience;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace UnitConverter.Core.Extensions;

public static class ResilienceExtensions
{
    /// <summary>
    /// Register resilience pipeline for database operations
    /// </summary>
    public static IServiceCollection AddDatabaseResiliencePipeline(
        this IServiceCollection services)
    {
        services.AddResiliencePipeline("DatabasePipeline", builder =>
        {
            builder
                // Retry transient DB failures
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromMilliseconds(200),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder()
                        .Handle<TimeoutRejectedException>()
                        // Add database-specific exceptions as needed
                })
                // Timeout if query takes too long
                .AddTimeout(TimeSpan.FromSeconds(5))
                // Circuit breaker to prevent cascading DB failures
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    MinimumThroughput = 5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    FailureRatio = 0.5
                });
        });

        return services;
    }

    /// <summary>
    /// Register enrichment for telemetry (traces, metrics, logs)
    /// </summary>
    public static IServiceCollection AddResilienceEnrichment(
        this IServiceCollection services)
    {
        services.AddResilienceEnricher();
        return services;
    }
}
```

**Usage**:
```csharp
// Invoke a resilient operation
public class UnitRepository : IUnitRepository
{
    private readonly ResiliencePipelineProvider<string> _pipelineProvider;

    public UnitRepository(ResiliencePipelineProvider<string> pipelineProvider)
    {
        _pipelineProvider = pipelineProvider;
    }

    public async Task<Unit?> GetByIdAsync(Guid id)
    {
        var pipeline = _pipelineProvider.GetPipeline("DatabasePipeline");
        return await pipeline.ExecuteAsync(async cancellationToken =>
        {
            // Your database query here
            return await _dbContext.Units.FindAsync(new object[] { id }, cancellationToken);
        });
    }
}
```

---

## 4. Monitoring & Observability

### 4.1 Resilience Telemetry

Polly v8 (underlying Resilience package) emits structured telemetry. We integrate with OpenTelemetry:

```csharp
// src/UnitConverter.Api/Program.cs
using OpenTelemetry.Metrics;
using OpenTelemetry.Traces;

var builder = WebApplicationBuilder.CreateBuilder(args);

// Add OpenTelemetry with Polly resilience enrichment
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            // Capture Polly resilience metrics
            .AddMeter("Polly")
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(builder.Configuration["Observability:Otlp:Endpoint"] ?? 
                                           "http://localhost:4317");
            });
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            // Trace Polly resilience events
            .AddSource("Polly")
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(builder.Configuration["Observability:Otlp:Endpoint"] ?? 
                                           "http://localhost:4317");
            });
    });

// Enable resilience enrichment (adds exception & request metadata)
builder.Services.AddResilienceEnricher();
```

### 4.2 Rate Limiting Metrics

Track rate limit rejections:

```csharp
// In Program.cs, the OnRejected handler already logs context
// OpenTelemetry will capture 429 responses as metrics

// Custom dashboard in Application Insights:
// - Count of 429 responses by policy and partition
// - P99 latency of successful requests
// - Circuit breaker state transitions
```

### 4.3 Example Queries (Application Insights KQL)

```kusto
// Rate limiting rejections by IP
requests
| where resultCode == 429
| extend ClientIp = tostring(client_IP)
| summarize RejectionCount = count() by ClientIp, bin(timestamp, 1m)
| order by RejectionCount desc

// Circuit breaker trips (look for 503 responses after a spike of failures)
requests
| where resultCode in (503, 504)
| where name contains "Catalog" or name contains "Conversion"
| summarize FailureCount = count(), AvgDuration = avg(duration) by name, bin(timestamp, 5m)

// Retry attempts (log event from Polly telemetry)
traces
| where message contains "OnRetry"
| summarize RetryCount = count() by operation_Name
| order by RetryCount desc
```

---

## 5. Testing Resilience

### 5.1 Unit Tests for Rate Limiting

```csharp
// tests/UnitConverter.Api.Tests/RateLimitingTests.cs
[TestClass]
public class RateLimitingTests
{
    [TestMethod]
    public async Task GetConvert_WhenExceedingRateLimit_Returns429()
    {
        var application = new WebApplicationFactory<Program>();
        var client = application.CreateClient();

        // Make 11 requests (limit is 10 per second)
        var tasks = Enumerable.Range(0, 11)
            .Select(_ => client.GetAsync("/api/convert?from=m&to=ft&value=1"))
            .ToList();

        await Task.WhenAll(tasks);

        var responses = tasks.Select(t => t.Result).ToList();
        
        // Expect at least one 429
        Assert.IsTrue(responses.Any(r => (int)r.StatusCode == 429),
            "Expected at least one rate limit rejection");
    }
}
```

### 5.2 Integration Tests for HTTP Resilience

```csharp
// tests/UnitConverter.Api.Tests/HttpResilienceTests.cs
using Moq;
using Polly.CircuitBreaker;
using System.Net.Http;

[TestClass]
public class HttpResilienceTests
{
    [TestMethod]
    public async Task CallCatalogService_WhenCircuitBreakerOpen_ReturnsFallback()
    {
        // Simulate circuit breaker open by throwing BrokenCircuitException
        var httpClient = new Mock<HttpClient>();
        httpClient.Setup(c => c.GetAsync(It.IsAny<string>()))
            .ThrowsAsync(new BrokenCircuitException("Circuit open"));

        // Assert that 503 is returned
        var controller = new UnitsController(httpClient.Object, Mock.Of<ILogger>());
        var result = await controller.GetUnitAsync(Guid.NewGuid());

        Assert.IsInstanceOfType(result, typeof(ObjectResult));
        var objectResult = (ObjectResult)result;
        Assert.AreEqual(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
    }
}
```

### 5.3 Load Tests with NBomber

```csharp
// perf/UnitConverter.Load.Tests/ResilienceLoadTests.cs
using NBomber.CSharp;

var httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:7000") };

var scenario = Scenario.Create("RateLimitingLoad", async context =>
{
    var request = new HttpRequestMessage(HttpMethod.Get, "/api/convert?from=m&to=ft&value=1");
    var response = await httpClient.SendAsync(request, context.CancellationToken);
    
    return response.StatusCode == System.Net.HttpStatusCode.OK
        ? Response.Ok()
        : Response.Fail($"Expected 200, got {response.StatusCode}");
})
.WithoutWarmup()
.WithLoadSimulations(
    Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
);

NBomberRunner
    .RegisterScenarios(scenario)
    .Run();

// Expected: ~10% 429 responses (rate limit kicks in at 10 req/sec per IP)
```

---

## 6. Deployment & Cloud Integration

### 6.1 Azure App Service Configuration

```json
{
  "ASPNETCORE_ENVIRONMENT": "Production",
  "Resilience:Retry:MaxRetryAttempts": "3",
  "Resilience:CircuitBreaker:FailureRatio": "0.5",
  "Resilience:CircuitBreaker:MinimumThroughput": "10",
  "RateLimiting:Policies:FixedWindow_ByIp:PermitLimit": "100",
  "Observability:Otlp:Endpoint": "https://observability-backend.azurewebsites.net"
}
```

### 6.2 Docker Compose (Local Testing)

```yaml
# docker-compose.yml
version: '3.8'
services:
  api:
    build: ./src/UnitConverter.Api
    ports:
      - "7000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - RateLimiting:Policies:FixedWindow_ByIp:PermitLimit=10
    depends_on:
      - catalog-service
      - conversion-service

  catalog-service:
    build: ./src/UnitConverter.Catalog
    ports:
      - "7001:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development

  conversion-service:
    build: ./src/UnitConverter.Conversion
    ports:
      - "7002:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
```

### 6.3 Kubernetes Deployment

```yaml
# k8s/api-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: unitconverter-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: api
  template:
    metadata:
      labels:
        app: api
    spec:
      containers:
      - name: api
        image: myregistry.azurecr.io/unitconverter-api:1.0
        resources:
          limits:
            memory: "512Mi"
            cpu: "500m"
          requests:
            memory: "256Mi"
            cpu: "250m"
        env:
        - name: RateLimiting__Policies__FixedWindow_ByIp__PermitLimit
          value: "100"
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 80
          initialDelaySeconds: 10
          periodSeconds: 5
```

---

## 7. Summary: Resilience Checklist

- [x] **Inbound**: `Microsoft.AspNetCore.RateLimiting` middleware configured (FixedWindow by IP, SlidingWindow by user, TokenBucket by API Key)
- [x] **Outbound HTTP**: `Microsoft.Extensions.Http.Resilience` standard handler with retry, circuit breaker, timeouts
- [x] **General Operations**: `Microsoft.Extensions.Resilience` for database & async calls
- [x] **Configuration**: Dynamic reloads enabled via `appsettings.json`
- [x] **Monitoring**: Polly telemetry integrated with OpenTelemetry
- [x] **Testing**: Unit tests, integration tests, load tests with NBomber
- [x] **Documentation**: Clear error messages for rate limit rejections (429)
- [x] **Deployment**: Cloud-ready with Azure App Service, Docker, Kubernetes examples

---

## 8. References

- [Building resilient cloud services with .NET 8](https://devblogs.microsoft.com/dotnet/building-resilient-cloud-services-with-dotnet-8/)
- [Introduction to resilient app development - .NET](https://learn.microsoft.com/en-us/dotnet/core/resilience/)
- [Polly Documentation](https://www.pollydocs.org/)
- [ASP.NET Core Rate Limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)
- [Azure DDoS Protection Standard](https://learn.microsoft.com/en-us/azure/ddos-protection/ddos-protection-standard-features)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
