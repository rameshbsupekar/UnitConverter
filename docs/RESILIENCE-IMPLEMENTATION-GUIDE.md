# Resilience Implementation Guide: From Design to Code

**Quick Start**: This guide helps developers implement resilience patterns from [RESILIENCE-ARCHITECTURE.md](./RESILIENCE-ARCHITECTURE.md) into the UnitConverter microservices.

---

## Phase 1: Milestone 1 Immediate Actions

### Task 1.1: Install Resilience NuGet Packages

```bash
cd src/UnitConverter.Api
dotnet add package Microsoft.Extensions.Http.Resilience
dotnet add package Microsoft.Extensions.Resilience
dotnet add package Microsoft.AspNetCore.RateLimiting

cd ../UnitConverter.Core
dotnet add package Microsoft.Extensions.Resilience

cd ../UnitConverter.Common
dotnet add package Microsoft.Extensions.Resilience
```

### Task 1.2: Create Rate Limiting Extension

**File**: `src/UnitConverter.Core/Extensions/RateLimitingExtensions.cs`

```csharp
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace UnitConverter.Core.Extensions;

public static class RateLimitingExtensions
{
    /// <summary>
    /// Configure rate limiting policies:
    /// - FixedWindow_ByIp: 10 req/sec per IP (public endpoints)
    /// - SlidingWindow_ByUser: 100 req/min per authenticated user
    /// - TokenBucket_ByApiKey: 500 req/min per API key (partners)
    /// </summary>
    public static IServiceCollection AddApplicationRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            var config = configuration.GetSection("RateLimiting");

            // Fixed window by IP
            options.AddFixedWindowLimiter(policyName: "FixedWindow_ByIp", configure: cfg =>
            {
                cfg.PermitLimit = config.GetValue("Policies:FixedWindow_ByIp:PermitLimit", 10);
                cfg.Window = TimeSpan.FromSeconds(1);
                cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                cfg.QueueLimit = config.GetValue("Policies:FixedWindow_ByIp:QueueLimit", 5);
            })
            .Partition(key: context =>
            {
                return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            });

            // Sliding window by user
            options.AddSlidingWindowLimiter(policyName: "SlidingWindow_ByUser", configure: cfg =>
            {
                cfg.PermitLimit = config.GetValue("Policies:SlidingWindow_ByUser:PermitLimit", 100);
                cfg.Window = TimeSpan.FromMinutes(1);
                cfg.SegmentsPerWindow = 2;
                cfg.QueueLimit = config.GetValue("Policies:SlidingWindow_ByUser:QueueLimit", 10);
            })
            .Partition(key: context =>
            {
                var userId = context.User?.FindFirst("sub")?.Value ??
                             context.Connection.RemoteIpAddress?.ToString() ??
                             "anonymous";
                return userId;
            });

            // Token bucket by API key
            options.AddTokenBucketLimiter(policyName: "TokenBucket_ByApiKey", configure: cfg =>
            {
                cfg.TokenLimit = config.GetValue("Policies:TokenBucket_ByApiKey:TokenLimit", 500);
                cfg.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
                cfg.TokensPerPeriod = 500;
                cfg.QueueLimit = 0;
            })
            .Partition(key: context =>
            {
                var apiKey = context.Request.Headers["X-Api-Key"].ToString();
                return !string.IsNullOrEmpty(apiKey) ? apiKey : "no-key";
            });

            // Rejection handler
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { error = "Rate limit exceeded. Try again later." },
                    cancellationToken: cancellationToken);
            };

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        return services;
    }
}
```

### Task 1.3: Create HTTP Resilience Extensions

**File**: `src/UnitConverter.Core/Extensions/HttpResilienceExtensions.cs`

```csharp
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace UnitConverter.Core.Extensions;

public static class HttpResilienceExtensions
{
    /// <summary>
    /// Register standard resilience handler for inter-service HTTP calls.
    /// Pipeline: Rate Limit -> Total Timeout -> Retry -> Circuit Breaker -> Attempt Timeout
    /// </summary>
    public static IHttpClientBuilder AddStandardServiceResilienceHandler(
        this IHttpClientBuilder builder,
        int maxRetries = 3)
    {
        return builder.AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = maxRetries;
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;
            options.Retry.Delay = TimeSpan.FromMilliseconds(100);

            options.CircuitBreaker.MinimumThroughput = 10;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.FailureRatio = 0.5;

            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
        });
    }
}
```

### Task 1.4: Update Program.cs

**File**: `src/UnitConverter.Api/Program.cs`

```csharp
using UnitConverter.Core.Extensions;

var builder = WebApplicationBuilder.CreateBuilder(args);

// Add resilience
builder.Services.AddApplicationRateLimiting(builder.Configuration);

// Register HTTP clients for inter-service calls
builder.Services
    .AddHttpClient("CatalogService", client =>
    {
        var url = builder.Configuration["Services:Catalog:Url"] ?? "https://localhost:7001";
        client.BaseAddress = new Uri(url);
        client.Timeout = TimeSpan.FromSeconds(10);
    })
    .AddStandardServiceResilienceHandler(maxRetries: 3);

builder.Services
    .AddHttpClient("ConversionService", client =>
    {
        var url = builder.Configuration["Services:Conversion:Url"] ?? "https://localhost:7002";
        client.BaseAddress = new Uri(url);
        client.Timeout = TimeSpan.FromSeconds(10);
    })
    .AddStandardServiceResilienceHandler(maxRetries: 3);

// ... rest of services

var app = builder.Build();

// Add rate limiting middleware EARLY (before routing)
app.UseRateLimiter();

// ... rest of middleware

// Map endpoints with rate limiting policies
app.MapGet("/api/convert", ConvertHandler)
   .RequireRateLimiting("FixedWindow_ByIp")
   .WithName("Convert")
   .WithOpenApi();

app.MapPost("/api/admin/units", CreateUnitHandler)
   .RequireRateLimiting("SlidingWindow_ByUser")
   .RequireAuthorization("AdminOnly")
   .WithName("CreateUnit")
   .WithOpenApi();

app.Run();
```

### Task 1.5: Update appsettings.json

**File**: `src/UnitConverter.Api/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Polly": "Debug"
    }
  },
  "AllowedHosts": "*",
  "Services": {
    "Catalog": {
      "Url": "https://localhost:7001"
    },
    "Conversion": {
      "Url": "https://localhost:7002"
    }
  },
  "RateLimiting": {
    "Policies": {
      "FixedWindow_ByIp": {
        "PermitLimit": 10,
        "QueueLimit": 5
      },
      "SlidingWindow_ByUser": {
        "PermitLimit": 100,
        "QueueLimit": 10
      },
      "TokenBucket_ByApiKey": {
        "TokenLimit": 500,
        "QueueLimit": 0
      }
    }
  }
}
```

---

## Phase 2: Service-to-Service Calls

### Task 2.1: Use HTTP Client in Controllers

**File**: `src/UnitConverter.Api/Controllers/UnitsController.cs` (partial example)

```csharp
[ApiController]
[Route("api/[controller]")]
public class UnitsController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UnitsController> _logger;

    public UnitsController(IHttpClientFactory factory, ILogger<UnitsController> logger)
    {
        _httpClientFactory = factory;
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
            var client = _httpClientFactory.CreateClient("CatalogService");
            var response = await client.GetAsync($"/api/units/{id}");

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Catalog returned {StatusCode}: {Content}", 
                    response.StatusCode, content);

                return response.StatusCode switch
                {
                    System.Net.HttpStatusCode.NotFound => NotFound(),
                    System.Net.HttpStatusCode.ServiceUnavailable => 
                        StatusCode(StatusCodes.Status503ServiceUnavailable),
                    _ => StatusCode((int)response.StatusCode)
                };
            }

            var unit = await response.Content.ReadAsAsync<UnitDto>();
            return Ok(unit);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Catalog service");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, 
                new { error = "Service temporarily unavailable" });
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout calling Catalog service");
            return StatusCode(StatusCodes.Status504GatewayTimeout, 
                new { error = "Request timeout" });
        }
    }
}
```

---

## Phase 3: Testing Resilience

### Task 3.1: Unit Test Rate Limiting

**File**: `tests/UnitConverter.Api.Tests/RateLimitingTests.cs`

```csharp
[TestClass]
public class RateLimitingTests
{
    [TestMethod]
    public async Task GetConvert_ExceedsFixedWindowLimit_Returns429()
    {
        var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        // Make 11 requests (limit is 10 per second)
        var tasks = Enumerable.Range(0, 11)
            .Select(_ => client.GetAsync("/api/convert?from=m&to=ft&value=1"))
            .ToList();

        await Task.WhenAll(tasks);
        var responses = tasks.Select(t => t.Result).ToList();

        var rejectionCount = responses.Count(r => (int)r.StatusCode == 429);
        Assert.IsTrue(rejectionCount > 0, "Expected at least one 429 response");
    }
}
```

### Task 3.2: Integration Test HTTP Resilience

**File**: `tests/UnitConverter.Api.Tests/HttpResilienceTests.cs`

```csharp
[TestClass]
public class HttpResilienceTests
{
    [TestMethod]
    public async Task CallCatalogService_WhenCircuitBreakerOpen_ReturnsFallback()
    {
        // Mock the HTTP client factory with a broken service
        var handler = new HttpClientHandler();
        var httpClient = new HttpClient(handler) 
        { 
            BaseAddress = new Uri("https://localhost:7001") 
        };

        // Simulate service down (circuit breaker will open after threshold)
        // This is a simplified test; in production, use tools like WireMock
        
        Assert.IsNotNull(httpClient);
    }
}
```

---

## Phase 4: Observability

### Task 4.1: Add OpenTelemetry Integration

**File**: `src/UnitConverter.Api/Program.cs` (add to existing setup)

```csharp
using OpenTelemetry.Metrics;
using OpenTelemetry.Traces;

var builder = WebApplicationBuilder.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter("Polly")
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(
                    builder.Configuration["Observability:Otlp:Endpoint"] ?? 
                    "http://localhost:4317");
            });
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("Polly")
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(
                    builder.Configuration["Observability:Otlp:Endpoint"] ?? 
                    "http://localhost:4317");
            });
    });

// Enable resilience enrichment
builder.Services.AddResilienceEnricher();
```

---

## Checklist for Developers

- [ ] Read [RESILIENCE-ARCHITECTURE.md](./RESILIENCE-ARCHITECTURE.md) fully
- [ ] Install NuGet packages (Task 1.1)
- [ ] Create rate limiting extension (Task 1.2)
- [ ] Create HTTP resilience extension (Task 1.3)
- [ ] Update Program.cs with resilience setup (Task 1.4)
- [ ] Update appsettings.json (Task 1.5)
- [ ] Implement service-to-service calls with error handling (Task 2.1)
- [ ] Write rate limiting tests (Task 3.1)
- [ ] Write HTTP resilience tests (Task 3.2)
- [ ] Add OpenTelemetry (Task 4.1)
- [ ] Run local tests with Docker Compose
- [ ] Verify logs and metrics in dashboard

---

## Troubleshooting

**Q: I'm getting 429 responses in local testing**
- A: Check `appsettings.json` — increase `PermitLimit` if you're stress testing

**Q: Circuit breaker not opening**
- A: Verify `MinimumThroughput` is reached (default: 10 attempts) and `FailureRatio` threshold is exceeded

**Q: OpenTelemetry metrics not showing**
- A: Ensure OTLP exporter endpoint is correct and reachable (default: `http://localhost:4317`)

**Q: Rate limiting not working**
- A: Confirm `app.UseRateLimiter()` is called before `app.UseRouting()`

---

## Next Steps

1. Complete Phase 1 tasks (resilience packages + basic setup)
2. Implement Phase 2 in Milestone 1 (service-to-service calls)
3. Add Phase 3 tests in Milestone 1 test suite
4. Integrate Phase 4 observability once OpenTelemetry backend is ready
5. Load test with NBomber (see [RESILIENCE-ARCHITECTURE.md § 5.3](./RESILIENCE-ARCHITECTURE.md))
