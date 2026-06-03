# Milestone 1 Auth Service: Updated with Resilience Patterns

**Date**: June 3, 2026  
**Scope**: Integration of resilience into Milestone 1 (Auth Service) implementation  
**Status**: Plan update complete

---

## Overview

The existing Milestone 1 Auth Service plan is enhanced with resilience patterns from the newly-created architecture documents. This ensures the Auth Service is production-grade from day one.

---

## Enhanced Milestone 1 Tasks (12 → 14 Tasks)

### Original Tasks (Completed)
- ✅ **Task 1**: Core Domain Models (User, Role, Email, UserId, Password)
- ✅ **Task 2**: Common Layer Interfaces (IRepository, ITokenGenerator, etc.)
- ✅ **Task 3**: JWT Token Service & Password Hashing
- ✅ **Task 4**: Register User Command Handler & Validation

### NEW Resilience Tasks (Added)
- 🆕 **Task 5a**: Rate Limiting Setup (NEW)
  - Add `Microsoft.AspNetCore.RateLimiting` NuGet package
  - Create `RateLimitingExtensions.cs` in `src/UnitConverter.Core`
  - Implement three rate limiter policies (FixedWindow by IP, SlidingWindow by user, TokenBucket by API key)
  - Update `appsettings.json` with rate limiting configuration
  - **Time**: 1.5 hours
  - **Tests**: Unit test for rate limiting middleware (1 test, ~50 lines)

- 🆕 **Task 5b**: HTTP Resilience Setup (NEW)
  - Add `Microsoft.Extensions.Http.Resilience` NuGet package
  - Create `HttpResilienceExtensions.cs` in `src/UnitConverter.Core`
  - Implement `AddStandardServiceResilienceHandler()` extension
  - Configure standard resilience pipeline (retry, circuit breaker, timeouts)
  - **Time**: 1.5 hours
  - **Tests**: Integration test for circuit breaker behavior (1 test, ~60 lines)

### Original Tasks (Continuing)
- **Task 6**: Database & EF Core Repositories
- **Task 7**: Login Command Handler
- **Task 8**: Refresh Token Handler
- **Task 9**: API Controllers (with rate limiting decorators)
- **Task 10**: Dependency Injection Setup (updated)
- **Task 11**: Security Middleware (updated)
- **Task 12**: Integration Tests
- **Task 13**: Docker Readiness

---

## Integration Points in Existing Tasks

### Task 9 (API Controllers) — NOW WITH RESILIENCE

**Before** (Original):
```csharp
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
    {
        // Register user
    }
}
```

**After** (With Resilience):
```csharp
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpPost("register")]
    [RequireRateLimiting("SlidingWindow_ByUser")]  // ← Rate limiting
    [ProduceResponseType(StatusCodes.Status201Created)]
    [ProduceResponseType(StatusCodes.Status429TooManyRequests)]  // ← Document rate limit response
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
    {
        // Register user
    }

    [HttpPost("login")]
    [RequireRateLimiting("FixedWindow_ByIp")]  // ← Stricter: 10 per sec per IP
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Login logic
    }

    [HttpPost("refresh-token")]
    [RequireRateLimiting("TokenBucket_ByApiKey")]  // ← For API partners
    [Authorize]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        // Refresh token logic
    }
}
```

**Rationale**:
- `/register`: Sliding window per user (prevents brute force registration)
- `/login`: Fixed window per IP (DDoS protection)
- `/refresh-token`: Token bucket per API key (partner integration)

---

### Task 10 (Dependency Injection Setup) — UPDATED

**File**: `src/UnitConverter.Auth/Auth.Api/Extensions/ServiceCollectionExtensions.cs`

```csharp
namespace UnitConverter.Auth.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthServices(this IServiceCollection services, IConfiguration config)
    {
        // 1. Add rate limiting (NEW)
        services.AddApplicationRateLimiting(config);

        // 2. Add HTTP resilience for external calls (NEW)
        services.AddHttpClient("ExternalAuthService")
            .AddStandardServiceResilienceHandler(maxRetries: 3);

        // 3. Add authentication services (existing)
        services.AddScoped<ITokenGenerator, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordService>();

        // 4. Add domain repositories (existing)
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();

        // ... rest of registration

        return services;
    }
}
```

**Usage in Program.cs**:
```csharp
var builder = WebApplicationBuilder.CreateBuilder(args);

// Add resilience + auth services
builder.Services.AddAuthServices(builder.Configuration);

// ... rest of configuration

var app = builder.Build();

// Add rate limiting middleware EARLY
app.UseRateLimiter();

// ... rest of middleware
```

---

### Task 11 (Security Middleware) — UPDATED WITH ERROR HANDLING

**File**: `src/UnitConverter.Auth/Auth.Api/Middleware/ErrorHandlingMiddleware.cs`

Now handles rate limiting rejections (429) explicitly:

```csharp
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (RateLimitException ex)  // ← NEW: Rate limiting exception
        {
            _logger.LogWarning("Rate limit exceeded for {Path} from {IP}", 
                context.Request.Path, 
                context.Connection.RemoteIpAddress);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded",
                retryAfter = 60  // Suggest retry after 60 seconds
            });
        }
        catch (TimeoutRejectedException ex)  // ← NEW: Timeout exception
        {
            _logger.LogError(ex, "Timeout occurred");
            context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
            // ... handle error
        }
        catch (BrokenCircuitException ex)  // ← NEW: Circuit breaker exception
        {
            _logger.LogError(ex, "Circuit breaker is open");
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            // ... handle error
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            // ... handle error
        }
    }
}
```

---

### Task 12 (Integration Tests) — ENHANCED

**New Test File**: `tests/UnitConverter.Auth.Tests/Integration/RateLimitingTests.cs`

```csharp
[TestClass]
public class AuthRateLimitingTests
{
    [TestMethod]
    public async Task Register_ExceedsSlidingWindowLimit_Returns429()
    {
        var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        // Make 101 requests as same user (limit is 100 per minute)
        var tasks = Enumerable.Range(0, 101)
            .Select(_ => client.PostAsJsonAsync("/api/auth/register", 
                new RegisterUserRequest { Email = "user@test.com", Password = "P@ssw0rd!" }))
            .ToList();

        await Task.WhenAll(tasks);
        var responses = tasks.Select(t => t.Result).ToList();

        var rejectionCount = responses.Count(r => (int)r.StatusCode == 429);
        Assert.IsTrue(rejectionCount > 0, "Expected at least one 429 response");
    }

    [TestMethod]
    public async Task Login_ExceedsFixedWindowLimit_Returns429()
    {
        var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        // Make 11 requests from same IP (limit is 10 per second)
        var tasks = Enumerable.Range(0, 11)
            .Select(_ => client.PostAsJsonAsync("/api/auth/login", 
                new LoginRequest { Email = "user@test.com", Password = "P@ssw0rd!" }))
            .ToList();

        await Task.WhenAll(tasks);
        var responses = tasks.Select(t => t.Result).ToList();

        var rejectionCount = responses.Count(r => (int)r.StatusCode == 429);
        Assert.IsTrue(rejectionCount > 0, "Expected at least one 429 response");
    }
}
```

---

## Updated Timeline for Milestone 1

| Phase | Task | Hours | Notes |
|-------|------|-------|-------|
| **Week 1** | 1-4 (Domain + Auth) | 16 | ✅ Completed |
| **Week 1-2** | 5a-5b (Resilience) | 3 | 🆕 NEW: Rate limiting + HTTP resilience |
| **Week 2** | 6-8 (DB + Handlers) | 12 | With resilience integration |
| **Week 2-3** | 9-11 (Controllers + Middleware) | 12 | With rate limiting decorators + error handling |
| **Week 3** | 12 (Tests) | 8 | 🔄 ENHANCED: Add rate limiting + resilience tests |
| **Week 3** | 13 (Docker) | 4 | With resilience configuration |
| **Total** | **All Tasks** | **55** | ⬆️ +3 hours from original 52 hours |

**Revised Milestone 1 Deadline**: ~3 weeks (keeping original timeline, just better coverage)

---

## Code Quality Improvements

### Rate Limiting Best Practices
✅ Partition by IP (DDoS), User (brute force), API Key (partner quota)  
✅ Different limits per endpoint (login stricter than register)  
✅ Graceful degradation (queue when possible, reject when necessary)  
✅ Metrics collection (count 429 responses)  

### HTTP Resilience Best Practices
✅ Standard pipeline (recommended by Microsoft)  
✅ Dynamic configuration reloads (no restart needed)  
✅ Explicit error handling (BrokenCircuitException, TimeoutRejectedException)  
✅ Observable (Polly telemetry + OpenTelemetry)  

---

## Observability Addition (Optional for Milestone 1)

If OpenTelemetry backend is available, add monitoring:

**In Program.cs**:
```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddMeter("Polly")
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(config["Observability:Otlp:Endpoint"] ?? 
                                           "http://localhost:4317");
            });
    });

builder.Services.AddResilienceEnricher();
```

**Metrics available in dashboard**:
- Count of 429 responses by policy and partition
- Latency distribution (P50, P95, P99)
- Circuit breaker state changes
- Retry attempts per endpoint

---

## Testing Coverage

**New Tests Added**:
- Rate limiting per endpoint (3 tests × 2 scenarios = 6 tests)
- Circuit breaker behavior (2 tests)
- Timeout handling (2 tests)
- Error responses (429, 503, 504)

**Total Tests for Milestone 1**: 260 + 10 = **270 tests**

---

## Deployment Configuration

### Docker Compose (Local Development)

**docker-compose.yml**:
```yaml
services:
  auth-api:
    build: ./src/UnitConverter.Auth/Auth.Api
    ports:
      - "7000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - RateLimiting__Policies__FixedWindow_ByIp__PermitLimit=10
      - RateLimiting__Policies__SlidingWindow_ByUser__PermitLimit=100
      - RateLimiting__Policies__TokenBucket_ByApiKey__TokenLimit=500
```

### Kubernetes (Production)

**StatefulSet replicas**: 3  
**Resource limits**: 512 MB / 500m CPU  
**Rate limiting**: Coordinated across replicas via local partitioning (no Redis needed initially)

---

## Summary

Milestone 1 is now **production-hardened** from day one with:

✅ **Inbound rate limiting** (3 partitioned policies)  
✅ **Outbound HTTP resilience** (standard Polly v8 pipeline)  
✅ **Explicit error handling** (429, 503, 504)  
✅ **Observable metrics** (Polly events to OpenTelemetry)  
✅ **Cloud-native ready** (Docker + Kubernetes config)  

**Time Investment**: +3 hours (minimal compared to long-term gains)  
**Quality Gain**: Production-grade resilience, DDoS protection, scalability  

---

## Next: Milestone 2 & 3

Once Milestone 1 is complete with resilience:

1. **Catalog Service** (Milestone 2): Replicate resilience patterns + add caching
2. **Conversion Service** (Milestone 3): Extreme scale + hedging for critical calls
3. **Message Queue** (Milestone 4+): Event-driven architecture with resilient consumers

All downstream services will follow the same resilience foundation established here.
