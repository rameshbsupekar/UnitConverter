# Refactoring Code Templates & Review Guide

**Purpose**: Show concrete examples of the refactoring before committing to full execution  
**Status**: Review these templates, then decide to proceed

---

## Template 1: Records for DTOs (Immutable, Shareable)

### Before (Class-based, in Auth.Application)
```csharp
// BEFORE: src/UnitConverter.Auth/Application/Commands/RegisterUserCommand.cs
public class RegisterUserCommand
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string OrganizationName { get; set; }
}

// BEFORE: src/UnitConverter.Auth/Application/DTOs/UserResponse.cs
public class UserResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string[] Roles { get; set; }
}
```

### After (Record-based, in UnitConverter.Contracts)
```csharp
// AFTER: src/UnitConverter.Contracts/Auth/Commands/RegisterUserCommand.cs
namespace UnitConverter.Contracts.Auth.Commands;

/// <summary>
/// Register a new user account
/// </summary>
public record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string OrganizationName
);

public record LoginCommand(
    string Email,
    string Password
);

public record RefreshTokenCommand(
    string RefreshToken
);

public record RevokeTokenCommand(
    string RefreshToken
);

// AFTER: src/UnitConverter.Contracts/Auth/Responses/UserResponse.cs
public record UserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string[] Roles
);

public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    DateTime IssuedAt
);

public record ErrorResponse(
    string Type,
    string Title,
    int Status,
    string Detail,
    string TraceId,
    string CorrelationId
);
```

**Benefits**:
- ✅ Immutable (thread-safe)
- ✅ Can be used across Auth.Api, Catalog.Api, Conversion.Api
- ✅ JSON serialization automatic
- ✅ Pattern matching friendly

---

## Template 2: Contracts Project Structure

### New Project Layout
```
src/
├── UnitConverter.Contracts/ (NEW)
│   ├── UnitConverter.Contracts.csproj
│   ├── Auth/
│   │   ├── Commands/
│   │   │   ├── RegisterUserCommand.cs
│   │   │   ├── LoginCommand.cs
│   │   │   ├── RefreshTokenCommand.cs
│   │   │   └── RevokeTokenCommand.cs
│   │   ├── Responses/
│   │   │   ├── UserResponse.cs
│   │   │   ├── TokenResponse.cs
│   │   │   └── ErrorResponse.cs
│   │   └── Interfaces/
│   │       └── ICatalogServiceClient.cs
│   │
│   ├── Catalog/
│   │   ├── Commands/
│   │   │   ├── SubmitUnitCommand.cs
│   │   │   └── ApproveUnitCommand.cs
│   │   └── Responses/
│   │       └── UnitResponse.cs
│   │
│   └── Conversion/
│       ├── Requests/
│       │   └── ConversionRequest.cs
│       └── Responses/
│           └── ConversionResponse.cs
```

### Project File (.csproj)
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <Description>Shared contracts, DTOs, and interfaces for UnitConverter microservices</Description>
  </PropertyGroup>

  <!-- NO external dependencies - only .NET framework -->

</Project>
```

**Key**: No dependencies! Just records and interfaces.

---

## Template 3: Interface-Based Resilience (Loose Coupling)

### Before (Tight Coupling)
```csharp
// BEFORE: Resilience mixed with business logic
public class ConversionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    public ConversionService(IHttpClientFactory factory)
    {
        _httpClientFactory = factory;
        // Rate limiting configuration here? NO!
    }
}
```

### After (Interface-Based, Loosely Coupled)
```csharp
// AFTER: src/UnitConverter.Core/Interfaces/IResilienceConfigurer.cs
namespace UnitConverter.Core.Interfaces;

public interface IResilienceConfigurer
{
    void AddRateLimiting(IServiceCollection services, IConfiguration config);
    void AddHttpResilience(IHttpClientBuilder builder);
}

// AFTER: src/UnitConverter.Core/Extensions/ResilienceExtensions.cs
public static class ResilienceExtensions
{
    public static IServiceCollection AddCoreResilience(
        this IServiceCollection services,
        IConfiguration config)
    {
        var configurer = new ResilienceConfigurer();
        configurer.AddRateLimiting(services, config);
        return services;
    }

    public static IHttpClientBuilder AddStandardServiceResilience(
        this IHttpClientBuilder builder)
    {
        return builder.AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
            options.CircuitBreaker.FailureRatio = 0.5;
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
        });
    }
}

// AFTER: Usage in Auth.Api/Program.cs (NO coupling to UnitConverter.Core internals!)
builder.Services.AddCoreResilience(builder.Configuration);

builder.Services
    .AddHttpClient("CatalogService", client => 
        client.BaseAddress = new Uri(config["Services:Catalog:Url"]))
    .AddStandardServiceResilience();
```

**Benefits**:
- ✅ UnitConverter.Core doesn't know about Auth, Catalog, Conversion
- ✅ Each service registers resilience independently
- ✅ Easy to swap implementations
- ✅ Tests can mock IResilienceConfigurer

---

## Template 4: Middleware Migration (From Core to API Layer)

### Before (Middleware in UnitConverter.Core)
```
src/UnitConverter.Core/
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs ← WRONG PLACE
│   ├── SecurityHeadersMiddleware.cs
│   ├── AuditLoggingMiddleware.cs
│   └── RequestLoggingMiddleware.cs
```

### After (Middleware in Auth.Api)
```
src/UnitConverter.Auth/
├── API/
│   ├── Middleware/
│   │   ├── ExceptionHandlingMiddleware.cs ← CORRECT PLACE
│   │   ├── SecurityHeadersMiddleware.cs
│   │   ├── AuditLoggingMiddleware.cs
│   │   ├── RequestLoggingMiddleware.cs
│   │   └── MiddlewareExtensions.cs
│   ├── Controllers/
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs
│   └── Program.cs
```

### Middleware Example
```csharp
// src/UnitConverter.Auth/API/Middleware/ExceptionHandlingMiddleware.cs
namespace UnitConverter.Auth.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error: {Message}", ex.Message);
            await WriteProblemDetails(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            await WriteProblemDetails(context, StatusCodes.Status401Unauthorized, "Unauthorized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteProblemDetails(context, StatusCodes.Status500InternalServerError, "Internal server error");
        }
    }

    private static async Task WriteProblemDetails(HttpContext context, int statusCode, string detail)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var problemDetails = new
        {
            type = $"https://api.unitconverter.dev/errors/{statusCode}",
            title = GetTitle(statusCode),
            status = statusCode,
            detail,
            traceId = context.TraceIdentifier,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        429 => "Too Many Requests",
        500 => "Internal Server Error",
        _ => "Error"
    };
}

// src/UnitConverter.Auth/API/Extensions/MiddlewareExtensions.cs
public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseAuthMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<AuditLoggingMiddleware>();
        return app;
    }
}

// src/UnitConverter.Auth/API/Program.cs
var app = builder.Build();

// Middleware in correct order
app.UseRateLimiter();           // ASP.NET Core middleware
app.UseAuthMiddleware();         // Custom auth middleware
app.UseRouting();
app.MapDefaultEndpoints();       // Health checks
app.MapAuthEndpoints();          // API endpoints

app.Run();
```

---

## Template 5: Factory Pattern for Cross-Service Communication

### Before (Tight Coupling)
```csharp
// BEFORE: Conversion service directly references Catalog
public class ConversionService
{
    private readonly CatalogServiceHttpClient _catalog;
    
    public ConversionService(CatalogServiceHttpClient catalog)
    {
        _catalog = catalog;
    }
}
```

### After (Factory Pattern, Loose Coupling)
```csharp
// src/UnitConverter.Contracts/Interfaces/ICatalogServiceClient.cs
namespace UnitConverter.Contracts.Interfaces;

public interface ICatalogServiceClient
{
    Task<UnitDto> GetUnitAsync(Guid unitId);
    Task<IEnumerable<UnitDto>> GetApprovedUnitsAsync();
    Task<CategoryDto> GetCategoryAsync(string categoryName);
}

// src/UnitConverter.Conversion/Infrastructure/Clients/CatalogServiceHttpClient.cs
namespace UnitConverter.Conversion.Infrastructure.Clients;

public class CatalogServiceHttpClient : ICatalogServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogServiceHttpClient> _logger;

    public CatalogServiceHttpClient(HttpClient httpClient, ILogger<CatalogServiceHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UnitDto> GetUnitAsync(Guid unitId)
    {
        var response = await _httpClient.GetAsync($"/api/v1/units/{unitId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsAsync<UnitDto>();
    }

    public async Task<IEnumerable<UnitDto>> GetApprovedUnitsAsync()
    {
        var response = await _httpClient.GetAsync("/api/v1/units?status=approved");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsAsync<IEnumerable<UnitDto>>();
    }

    public async Task<CategoryDto> GetCategoryAsync(string categoryName)
    {
        var response = await _httpClient.GetAsync($"/api/v1/categories/{categoryName}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsAsync<CategoryDto>();
    }
}

// src/UnitConverter.Conversion/Infrastructure/Factories/ServiceClientFactory.cs
public static class ServiceClientFactory
{
    public static ICatalogServiceClient CreateCatalogClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration config)
    {
        var client = httpClientFactory.CreateClient("CatalogService");
        return new CatalogServiceHttpClient(client, logger);
    }
}

// src/UnitConverter.Conversion/Program.cs (Usage)
// Register the HTTP client with resilience
builder.Services
    .AddHttpClient("CatalogService", client =>
        client.BaseAddress = new Uri(config["Services:Catalog:Url"]))
    .AddStandardServiceResilience();

// Register the service client using factory
builder.Services.AddScoped<ICatalogServiceClient>(sp =>
    ServiceClientFactory.CreateCatalogClient(
        sp.GetRequiredService<IHttpClientFactory>(),
        builder.Configuration));

// In your handler: NO reference to CatalogServiceHttpClient!
public class ConversionHandler
{
    private readonly ICatalogServiceClient _catalog; // ← Interface only!

    public ConversionHandler(ICatalogServiceClient catalog)
    {
        _catalog = catalog;
    }

    public async Task<ConversionResponse> HandleAsync(ConversionRequest request)
    {
        var unit = await _catalog.GetUnitAsync(request.ToUnitId);
        // Convert...
    }
}
```

**Benefits**:
- ✅ Conversion service only depends on IServiceClient interface
- ✅ No direct reference to Catalog.Api
- ✅ Easy to mock in tests
- ✅ Can swap HTTP → gRPC without changing code

---

## Template 6: LocalDB Configuration

### appsettings.Development.json (NEW)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "ConnectionStrings": {
    "AuthDb": "Server=(localdb)\\mssqllocaldb;Database=UnitConverter.Auth.Dev;Trusted_Connection=true;Encrypt=false"
  },
  "Aspire": {
    "UseLocalDb": true,
    "EnsureMigrationsApplied": true
  },
  "Services": {
    "Catalog": {
      "Url": "https://localhost:7001"
    },
    "Conversion": {
      "Url": "https://localhost:7002"
    }
  }
}
```

### appsettings.json (Production)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "AuthDb": "Server=auth-db-server;Database=UnitConverter.Auth;Encrypt=true;TrustServerCertificate=false"
  },
  "Aspire": {
    "UseLocalDb": false,
    "EnsureMigrationsApplied": true
  },
  "Services": {
    "Catalog": {
      "Url": "https://catalog-api.internal"
    },
    "Conversion": {
      "Url": "https://conversion-api.internal"
    }
  }
}
```

### Program.cs Configuration
```csharp
// src/UnitConverter.Auth/API/Program.cs
var builder = WebApplicationBuilder.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("AuthDb");
var useLocalDb = builder.Configuration.GetValue<bool>("Aspire:UseLocalDb");
var ensureMigrations = builder.Configuration.GetValue<bool>("Aspire:EnsureMigrationsApplied");

// Register DbContext
builder.Services.AddDbContext<AuthDbContext>(options =>
{
    if (useLocalDb)
    {
        options.UseSqlServer(connectionString);
        options.EnableSensitiveDataLogging(true); // Only in dev!
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

var app = builder.Build();

// Apply migrations automatically
if (ensureMigrations)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
```

### Test DbContext (SQLite In-Memory)
```csharp
// tests/UnitConverter.Auth.Tests/Fixtures/DatabaseFixture.cs
public class DatabaseFixture : IAsyncLifetime
{
    private readonly string _connectionString = "Data Source=:memory:";
    private SqliteConnection _connection;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection(_connectionString);
        await _connection.OpenAsync();

        var contextOptions = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new AuthDbContext(contextOptions);
        await context.Database.EnsureCreatedAsync();
    }

    public DbContextOptions<AuthDbContext> GetOptions()
    {
        return new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite(_connection)
            .Options;
    }

    public async Task DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
    }
}
```

---

## Review Checklist

Before you decide to proceed with the full refactoring, review these templates:

### Design Patterns ✅
- [ ] Records pattern makes sense (immutable DTOs)
- [ ] Contracts project is the right place for shared types
- [ ] Interface-based resilience keeps UnitConverter.Core decoupled
- [ ] Factory pattern is clear for cross-service calls
- [ ] Middleware in API layer makes sense (not shared library)

### Technical Feasibility ✅
- [ ] Records work with JSON serialization
- [ ] LocalDB + SQLite dual config is practical
- [ ] EF Core migrations apply automatically
- [ ] All patterns are .NET 10+ compatible

### Reusability ✅
- [ ] Contracts project can be shared across all services
- [ ] IResilienceConfigurer pattern is extensible
- [ ] ICatalogServiceClient is easy to mock in tests
- [ ] Middleware is self-contained per service

### Code Quality ✅
- [ ] No circular dependencies
- [ ] Interface segregation (small focused interfaces)
- [ ] Dependency injection is clean
- [ ] Exception handling is centralized

---

## Questions to Consider

1. **Records**: Do you prefer immutable DTOs, or stick with classes?
2. **Contracts**: Is a separate project the right approach for your team?
3. **Middleware**: Comfortable moving all 4 middleware to Auth.Api?
4. **LocalDB**: Is developer experience with `(localdb)\mssqllocaldb` good enough?
5. **Factory Pattern**: Clear enough for cross-service calls?

---

## Decision Point

If these templates look good:
- **"Yes, proceed with refactoring"** → Launch R1-R5 tasks (2.5 hours)
- **"Show me more examples"** → I can provide additional patterns
- **"Modify approach"** → Tell me what needs changing
- **"Skip refactoring"** → Go straight to Tasks 7-13

What's your verdict? 👇
