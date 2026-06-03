# Architectural Refactoring Plan: Library Reusability, Records, and LocalDB

**Date**: June 3, 2026  
**Scope**: Redesign for maximum library reusability + LocalDB support + Modern .NET 8+ patterns  
**Impact**: Tasks 1-6 refactoring + architectural improvements

---

## Current Issues to Fix

### Issue 1: LocalDB vs SQL Server
**Problem**: Current code uses SQL Server; developers without SSMS can't run tests  
**Solution**: Configure EF Core to use LocalDB (Windows) or SQLite (cross-platform) for local development

### Issue 2: DTOs in Implementation Projects
**Problem**: RegisterUserCommand, etc., are in Auth.Application; can't reuse in other microservices  
**Solution**: Move to contract projects as **C# records** for immutability + easy sharing

### Issue 3: Middleware in Shared Libraries
**Problem**: Middleware (security, exceptions) in UnitConverter.Core; belongs in API layer  
**Solution**: Move middleware to individual API projects (Auth.Api, Catalog.Api, etc.)

### Issue 4: Library Over-References
**Problem**: UnitConverter.Core references Domain; UnitConverter.ServiceDefaults references Auth  
**Solution**: Use **interface-based design** + **factory/adapter patterns** to decouple

### Issue 5: Missing Contracts Projects
**Problem**: No dedicated contracts/shared DTO projects  
**Solution**: Create `UnitConverter.Contracts.*` projects for each service

---

## Refactored Architecture

### Project Structure (New)

```
src/
├── UnitConverter.Contracts/                    # ⭐ NEW: Shared records + interfaces
│   ├── Auth/
│   │   ├── Requests/RegisterUserRequest.cs    # record
│   │   ├── Requests/LoginRequest.cs           # record
│   │   ├── Requests/RefreshTokenRequest.cs    # record
│   │   ├── Responses/TokenResponse.cs         # record
│   │   ├── Responses/UserResponse.cs          # record
│   │   ├── Commands/RegisterUserCommand.cs    # record (not Command pattern)
│   │   ├── Commands/LoginCommand.cs           # record
│   │   └── Interfaces/ (if needed for loose coupling)
│   │
│   ├── Catalog/
│   │   ├── Requests/CreateUnitRequest.cs
│   │   ├── Responses/UnitResponse.cs
│   │   └── Commands/SubmitUnitCommand.cs
│   │
│   └── Conversion/
│       ├── Requests/ConversionRequest.cs
│       └── Responses/ConversionResponse.cs
│
├── UnitConverter.Core/                         # ⭐ REFACTORED: Resilience only
│   ├── Extensions/
│   │   ├── RateLimitingExtensions.cs
│   │   └── HttpResilienceExtensions.cs
│   └── Models/ (configuration only, NO business logic)
│
├── UnitConverter.ServiceDefaults/              # ⭐ REFACTORED: Health + OTel
│   ├── Extensions.cs
│   └── Models/
│
├── UnitConverter.Auth/
│   ├── Core/
│   │   ├── Domain/ (entities, value objects, exceptions)
│   │   └── NO references to Contracts or other services
│   │
│   ├── Application/
│   │   ├── Commands/ (business logic commands)
│   │   ├── Handlers/
│   │   ├── Services/
│   │   ├── Validators/
│   │   └── NO DTOs (use Contracts)
│   │
│   ├── Infrastructure/
│   │   ├── Data/ (DbContext, repositories)
│   │   ├── Extensions/
│   │   └── NO references to UI/controllers
│   │
│   └── API/ (Auth.API or just Auth.Api) ⭐ NEW LAYER
│       ├── Controllers/
│       ├── Middleware/ ⭐ MOVED HERE (was in Core)
│       │   ├── ExceptionHandlingMiddleware.cs
│       │   ├── SecurityHeadersMiddleware.cs
│       │   ├── AuditLoggingMiddleware.cs
│       │   └── RequestLoggingMiddleware.cs
│       ├── Extensions/
│       │   ├── ServiceCollectionExtensions.cs (DI for THIS service only)
│       │   └── MiddlewareExtensions.cs
│       ├── Program.cs
│       └── appsettings.json (LocalDB connection string)
│
├── UnitConverter.Catalog/ (future)
├── UnitConverter.Conversion/ (future)
└── UnitConverter.Web/

tests/
├── UnitConverter.Core.Tests/
├── UnitConverter.ServiceDefaults.Tests/
├── UnitConverter.Auth.Tests/
│   ├── Unit/
│   ├── Integration/
│   └── Fixtures/
└── UnitConverter.Contracts.Tests/ (schema/contract validation)
```

---

## Key Design Pattern Changes

### Pattern 1: Records for DTOs & Commands

**Before** (class-based):
```csharp
// In Auth.Application (Not shareable)
public class RegisterUserCommand
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class UserResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; }
}
```

**After** (record-based, in Contracts):
```csharp
// In UnitConverter.Contracts.Auth/Commands/
public record RegisterUserCommand(
    string Email,
    string Password
);

public record LoginCommand(
    string Email,
    string Password
);

public record RefreshTokenCommand(
    string RefreshToken
);

// In UnitConverter.Contracts.Auth/Responses/
public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    DateTime IssuedAt
);

public record UserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string[] Roles
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
- ✅ Pattern matching friendly
- ✅ Shareable across services
- ✅ JSON serialization automatic
- ✅ Value equality built-in

---

### Pattern 2: Interface-Based Loose Coupling

**Library Design**: Make libraries NOT depend on concrete implementations

**Example 1: Resilience Library (UnitConverter.Core)**

```csharp
// UnitConverter.Core/Interfaces/IResilienceConfigurer.cs
public interface IResilienceConfigurer
{
    void ConfigureRateLimiting(IServiceCollection services, IConfiguration config);
    void ConfigureHttpResilience(IHttpClientBuilder builder);
}

// UnitConverter.Core/Extensions/ResilienceServiceCollectionExtensions.cs
public static class ResilienceServiceCollectionExtensions
{
    public static IServiceCollection AddCoreResilience(
        this IServiceCollection services,
        IConfiguration config,
        Action<IResilienceConfigurer> configure = null)
    {
        var configurer = new ResilienceConfigurer();
        configurer.ConfigureRateLimiting(services, config);
        
        configure?.Invoke(configurer);
        
        return services;
    }
}

// UnitConverter.Auth.Api/Program.cs - Usage (NO reference to Core internals!)
builder.Services.AddCoreResilience(builder.Configuration, options =>
{
    // Optional customization
});
```

**Benefits**:
- ✅ UnitConverter.Core has NO dependencies on Auth, Catalog, etc.
- ✅ Can swap implementations without changing library code
- ✅ Each service registers resilience independently
- ✅ Tests can mock resilience configuration

---

### Pattern 3: Adapter Pattern for Middleware

**Problem**: Middleware needs logging, but shouldn't reference concrete logger  
**Solution**: Adapter pattern via ILogger

```csharp
// In Auth.Api/Middleware/
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;  // ← Abstraction!

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;  // ILogger is already abstracted by DI
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            // Convert to ProblemDetails...
        }
    }
}
```

**Key Point**: Use `ILogger<T>` (interface), never concrete logger implementations → loose coupling

---

### Pattern 4: Factory Pattern for Cross-Service Communication

**Problem**: Conversion Service needs to call Catalog Service, but shouldn't reference Catalog directly  
**Solution**: Factory + interface

```csharp
// UnitConverter.Contracts/Interfaces/ICatalogServiceClient.cs
public interface ICatalogServiceClient
{
    Task<UnitDto> GetUnitAsync(Guid unitId);
    Task<IEnumerable<UnitDto>> GetApprovedUnitsAsync();
}

// UnitConverter.Conversion.Api/Clients/CatalogServiceClientFactory.cs
public static class CatalogServiceClientFactory
{
    public static ICatalogServiceClient Create(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        var client = httpClientFactory.CreateClient("CatalogService");
        return new CatalogServiceHttpClient(client, config);
    }
}

// Usage in Program.cs
builder.Services.AddScoped<ICatalogServiceClient>(sp =>
    CatalogServiceClientFactory.Create(
        sp.GetRequiredService<IHttpClientFactory>(),
        builder.Configuration)
);
```

**Benefits**:
- ✅ No direct reference to Catalog.Api
- ✅ Easy to mock in tests
- ✅ Interface-based contract
- ✅ Can swap HTTP → gRPC without changing code

---

## LocalDB Configuration

### Setup 1: Windows Local Development (LocalDB)

**File**: `src/UnitConverter.Auth/API/appsettings.Development.json` (new)

```json
{
  "ConnectionStrings": {
    "AuthDb": "Server=(localdb)\\mssqllocaldb;Database=UnitConverter.Auth.Dev;Trusted_Connection=true;Encrypt=false"
  },
  "Aspire": {
    "UseLocalDb": true
  }
}
```

**File**: `src/UnitConverter.Auth/API/appsettings.json` (production default)

```json
{
  "ConnectionStrings": {
    "AuthDb": "Server=sql-server;Database=UnitConverter.Auth;User Id=sa;Password=YourSecurePassword"
  }
}
```

### Setup 2: Program.cs Configuration

```csharp
// Auth.Api/Program.cs
var builder = WebApplicationBuilder.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("AuthDb");
var useLocalDb = builder.Configuration.GetValue<bool>("Aspire:UseLocalDb");

if (useLocalDb)
{
    // Ensure LocalDB instance exists
    EnsureLocalDbExists();
}

builder.Services.AddDbContext<AuthDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// Helper method
static void EnsureLocalDbExists()
{
    // Optional: auto-create LocalDB if needed
    // This would use sqlcmd or similar
}
```

### Setup 3: Tests (SQLite)

**File**: `tests/UnitConverter.Auth.Tests/DatabaseFixture.cs`

```csharp
public class DatabaseFixture : IAsyncLifetime
{
    private readonly string _connectionString;
    private SqliteConnection _connection;

    public DatabaseFixture()
    {
        _connectionString = "Data Source=:memory:";
    }

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection(_connectionString);
        await _connection.OpenAsync();

        using var context = new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite(_connection)
            .Options);

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

// Usage in tests:
[Trait("Category", "Integration")]
public class UserRepositoryTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private AuthDbContext _context;

    public UserRepositoryTests()
    {
        _fixture = new DatabaseFixture();
    }

    public async Task InitializeAsync() => await _fixture.InitializeAsync();
    public async Task DisposeAsync() => await _fixture.DisposeAsync();

    [Fact]
    public async Task GetByEmailAsync_WithValidEmail_ReturnsUser()
    {
        _context = new AuthDbContext(_fixture.GetOptions());
        var repo = new UserRepository(_context);

        var user = await repo.GetByEmailAsync("test@example.com");
        Assert.NotNull(user);
    }
}
```

---

## Refactoring Tasks (Immediate)

### Task R1: Create Contracts Projects (1 hour)

- [ ] Create `UnitConverter.Contracts/` project (csproj)
- [ ] Create Auth contracts:
  - [ ] RegisterUserRequest.cs (record)
  - [ ] LoginRequest.cs (record)
  - [ ] RefreshTokenRequest.cs (record)
  - [ ] TokenResponse.cs (record)
  - [ ] UserResponse.cs (record)
  - [ ] ErrorResponse.cs (record)
- [ ] Move RegisterUserCommand here (as record)
- [ ] Update Auth.Application to use contracts

### Task R2: Refactor DTOs to Records (1 hour)

- [ ] Convert all DTOs to records
- [ ] Update serialization (System.Text.Json supports records natively)
- [ ] Update tests to use record construction
- [ ] Verify JSON round-trip works

### Task R3: Move Middleware to Auth.Api (1.5 hours)

- [ ] Create `Auth.Api/Middleware/` folder
- [ ] Move ExceptionHandlingMiddleware from Core
- [ ] Move SecurityHeadersMiddleware from Core
- [ ] Move AuditLoggingMiddleware from Core
- [ ] Move RequestLoggingMiddleware from Core
- [ ] Update Auth.Api Program.cs to register middleware

### Task R4: Decouple UnitConverter.Core (1 hour)

- [ ] Remove any references to Auth, Catalog, Conversion from Core
- [ ] Move rate limiting configuration to interface-based design
- [ ] Create IResilienceConfigurer interface
- [ ] Verify Core only depends on Microsoft.Extensions.*

### Task R5: Configure LocalDB Support (1 hour)

- [ ] Create `appsettings.Development.json` with LocalDB connection string
- [ ] Update `appsettings.json` for production SQL Server
- [ ] Update Program.cs to handle both connection types
- [ ] Test locally without SSMS (just `dotnet run`)

### Task R6: Update EF Core DbContext (30 min)

- [ ] Verify DbContext uses injected connection string
- [ ] Test migrations work with both LocalDB and SQLite
- [ ] Ensure tests use SQLite (in-memory)

---

## Refactoring Impact Summary

| Aspect | Before | After |
|--------|--------|-------|
| **Contracts** | In Application layer | Separate Contracts project |
| **DTOs** | Classes | **Records** (immutable) |
| **Middleware** | In Core library | In Api layer |
| **Database** | SQL Server only | **LocalDB + SQLite support** |
| **Library coupling** | Tight (Core refs Auth) | **Loose (interface-based)** |
| **Reusability** | Limited | **High** |
| **Pattern** | Ad-hoc | **Factory, Adapter, Interface** |

---

## Modern .NET 8+ Integration

### Already Implemented ✅
- Microsoft.Extensions.Http.Resilience (Polly v8)
- Microsoft.Extensions.Resilience
- Microsoft.AspNetCore.RateLimiting
- OpenTelemetry integration
- Health checks (ServiceDefaults)

### To Add (Optional Cloud-Native)
- **AspNetCore.HealthChecks**: UI dashboard for health checks
- **Microsoft.Extensions.Diagnostics.ResourceMonitoring**: CPU/memory monitoring
- **.NET Aspire**: Full orchestration + developer dashboard

### Example: Full Cloud-Native Setup

```csharp
// Program.cs
var builder = WebApplicationBuilder.CreateBuilder(args);

// 1. Contracts layer (shared DTOs)
// (comes from UnitConverter.Contracts package)

// 2. Core infrastructure
builder.AddServiceDefaults();  // Health + OTel
builder.AddCoreResilience(builder.Configuration);  // Rate limiting + HTTP resilience

// 3. Business logic
builder.Services.AddAuthServices();

// 4. Database
builder.Services.AddDbContext<AuthDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("AuthDb"));
});

// 5. API & middleware
var app = builder.Build();

app.UseRateLimiter();
app.UseExceptionHandlingMiddleware();
app.UseSecurityHeadersMiddleware();
app.UseAuditLoggingMiddleware();
app.UseRequestLoggingMiddleware();

app.MapDefaultEndpoints();  // /health, /alive
app.MapAuthEndpoints();

app.Run();
```

---

## Refactoring Execution Order

**Phase 1** (1 hour): Create Contracts projects + records
**Phase 2** (1.5 hours): Move middleware to Api layer
**Phase 3** (1 hour): Decouple UnitConverter.Core
**Phase 4** (1 hour): LocalDB configuration
**Phase 5** (30 min): EF Core updates

**Total**: ~4.5 hours

**Parallel**: Can do Phases 1, 2, 3 in parallel (2.5 hours)

---

## Success Criteria

- ✅ All DTOs as records in Contracts project
- ✅ Middleware in Auth.Api (not Core)
- ✅ UnitConverter.Core has no references to business logic
- ✅ Interface-based design throughout
- ✅ LocalDB works locally without SSMS
- ✅ SQLite works in tests
- ✅ All existing tests pass (198+)
- ✅ New architecture documented

---

## Next Steps

1. **Confirm refactoring plan**: Does this architecture match your vision?
2. **Execute Phase 1-5**: Refactor existing code
3. **Resume Tasks 7-13**: Implement remaining handlers using new structure
4. **Milestone 1 completion**: Working Auth Service with refactored architecture

Ready to proceed with refactoring?
