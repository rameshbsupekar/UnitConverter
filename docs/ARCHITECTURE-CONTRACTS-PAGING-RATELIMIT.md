# Refactored Microservices Architecture: Contracts + Implementations with Paging & Rate Limiting

## Architecture: Contracts Separation Pattern

```
src/
├── UnitConverter.Core.Contracts/              # ⭐ Shared core contracts ONLY
│   ├── Interfaces/
│   │   ├── IRepository.cs
│   │   ├── IUnitOfWork.cs
│   │   ├── IPagedList.cs                      # Paging contract
│   │   ├── IRateLimiter.cs                    # Rate limiting contract
│   │   └── IDataProtector.cs
│   ├── Models/
│   │   ├── PagedRequest.cs                    # Paging request model
│   │   ├── PagedResult.cs                     # Paging result model
│   │   └── RateLimitPolicy.cs                 # Rate limit policy
│   └── Constants/
│       └── CoreConstants.cs
│   (NO ServiceCollectionExtensions here - only contracts!)
│
├── UnitConverter.Domain.Contracts/            # ⭐ Shared domain contracts ONLY
│   ├── Interfaces/
│   │   ├── ITokenGenerator.cs
│   │   ├── IPasswordHasher.cs
│   │   ├── IAuthenticationService.cs
│   │   └── IUnitService.cs                    # Domain service contract
│   ├── Models/
│   │   ├── TokenValidationResult.cs
│   │   └── AuthenticationContext.cs
│   └── Constants/
│       └── DomainConstants.cs
│   (NO ServiceCollectionExtensions here - only contracts!)
│
├── UnitConverter.Core/                        # ⭐ Core implementations
│   ├── Services/
│   │   ├── RateLimiter.cs                     # Implements IRateLimiter
│   │   ├── PagingService.cs                   # Implements IPagedList logic
│   │   └── DataProtectionService.cs           # Implements IDataProtector
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs    # ⭐ Registers: RateLimiter, DataProtector
│
├── UnitConverter.Common/                      # ⭐ Common implementations
│   ├── Services/
│   │   ├── JwtTokenService.cs                 # Implements ITokenGenerator
│   │   └── PasswordService.cs                 # Implements IPasswordHasher
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs    # ⭐ Registers: JWT, Password
│
├── UnitConverter.Auth/                        # Auth microservice
│   ├── Auth.Domain/                           # Domain implementations
│   │   ├── Repositories/
│   │   │   ├── UserRepository.cs              # Implements IUserRepository
│   │   │   └── RoleRepository.cs              # Implements IRoleRepository
│   │   ├── Services/
│   │   │   └── AuthenticationService.cs       # Implements IAuthenticationService
│   │   └── Extensions/
│   │       └── ServiceCollectionExtensions.cs # ⭐ Registers: Repositories, AuthService
│   │
│   ├── Auth.Application/                      # Application layer
│   │   ├── Commands/
│   │   ├── Handlers/
│   │   ├── Validators/
│   │   └── Extensions/
│   │       └── ServiceCollectionExtensions.cs # ⭐ Registers: Handlers, Validators
│   │
│   ├── Auth.Infrastructure/                   # Infrastructure (EF Core)
│   │   ├── Data/
│   │   ├── Repositories/
│   │   └── Extensions/
│   │       └── ServiceCollectionExtensions.cs # ⭐ Registers: DbContext, EF Repos
│   │
│   └── Auth.API/
│       ├── Controllers/
│       └── Extensions/
│           └── ServiceCollectionExtensions.cs # ⭐ MASTER: Calls all layers
│
└── (Repeat for UnitConverter.Catalog, UnitConverter.Conversion)
```

---

## Key Pattern: Service Registration in Implementation Projects

### ✅ Contracts Project (Read-Only)
```csharp
// UnitConverter.Core.Contracts/Interfaces/IRateLimiter.cs
namespace UnitConverter.Core.Contracts.Interfaces;

public interface IRateLimiter
{
    Task<bool> IsAllowedAsync(string key, int requestsPerMinute);
}
```

**NO ServiceCollectionExtensions in contracts!**

### ✅ Implementation Project (With Registration)
```csharp
// UnitConverter.Core/Services/RateLimiter.cs
using UnitConverter.Core.Contracts.Interfaces;

namespace UnitConverter.Core.Services;

public class RateLimiter : IRateLimiter
{
    private readonly IMemoryCache _cache;
    
    public RateLimiter(IMemoryCache cache)
    {
        _cache = cache;
    }
    
    public async Task<bool> IsAllowedAsync(string key, int requestsPerMinute)
    {
        // Implementation
        var count = _cache.GetOrCreate(key, entry => 
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return 0;
        });
        
        if (count >= requestsPerMinute)
            return false;
        
        _cache.Set(key, count + 1, TimeSpan.FromMinutes(1));
        return true;
    }
}

// ⭐ ServiceCollectionExtensions IN IMPLEMENTATION PROJECT
namespace UnitConverter.Core.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Core layer implementations.
    /// Call in Program.cs or from Auth.API master registration.
    /// </summary>
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddScoped<IRateLimiter, RateLimiter>();
        services.AddScoped<IDataProtector, DataProtectionService>();
        
        return services;
    }
}
```

---

## Paging Support for Large Datasets

### Contracts
```csharp
// UnitConverter.Core.Contracts/Interfaces/IPagedList.cs
namespace UnitConverter.Core.Contracts.Interfaces;

public interface IPagedList<T>
{
    int PageNumber { get; }
    int PageSize { get; }
    int TotalCount { get; }
    int TotalPages { get; }
    bool HasPreviousPage { get; }
    bool HasNextPage { get; }
    IReadOnlyList<T> Items { get; }
}

// UnitConverter.Core.Contracts/Models/PagedRequest.cs
namespace UnitConverter.Core.Contracts.Models;

public sealed class PagedRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public bool Descending { get; set; }
    
    public void Validate()
    {
        if (PageNumber < 1) PageNumber = 1;
        if (PageSize < 1) PageSize = 1;
        if (PageSize > 100) PageSize = 100;  // Max 100 items per page
    }
}

// UnitConverter.Core.Contracts/Models/PagedResult.cs
namespace UnitConverter.Core.Contracts.Models;

public sealed class PagedResult<T> : IPagedList<T>
{
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    public IReadOnlyList<T> Items { get; }

    public PagedResult(List<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items.AsReadOnly();
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public static PagedResult<T> Create(IQueryable<T> query, int pageNumber, int pageSize)
    {
        var totalCount = query.Count();
        var items = query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        
        return new PagedResult<T>(items, pageNumber, pageSize, totalCount);
    }
}
```

### Implementation in Repository
```csharp
// UnitConverter.Auth.Domain/Repositories/UserRepository.cs
using UnitConverter.Core.Contracts.Interfaces;
using UnitConverter.Core.Contracts.Models;

namespace UnitConverter.Auth.Domain.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _context;
    
    public async Task<IPagedList<User>> GetPagedAsync(PagedRequest request)
    {
        request.Validate();
        
        var query = _context.Users.AsQueryable();
        
        // Apply sorting
        query = request.SortBy switch
        {
            "email" => request.Descending 
                ? query.OrderByDescending(u => u.Email.Value)
                : query.OrderBy(u => u.Email.Value),
            "createdAt" => request.Descending
                ? query.OrderByDescending(u => u.CreatedAt)
                : query.OrderBy(u => u.CreatedAt),
            _ => query.OrderByDescending(u => u.CreatedAt)  // Default
        };
        
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();
        
        return new PagedResult<User>(items, request.PageNumber, request.PageSize, totalCount);
    }
}
```

---

## Rate Limiting for GET Requests (Security)

### Contracts
```csharp
// UnitConverter.Core.Contracts/Models/RateLimitPolicy.cs
namespace UnitConverter.Core.Contracts.Models;

public sealed class RateLimitPolicy
{
    public string Key { get; }
    public int RequestsPerMinute { get; }
    public string? ErrorMessage { get; }

    public RateLimitPolicy(string key, int requestsPerMinute, string? errorMessage = null)
    {
        Key = key;
        RequestsPerMinute = requestsPerMinute;
        ErrorMessage = errorMessage ?? $"Rate limit exceeded: {requestsPerMinute} requests per minute";
    }
}

// Common rate limit policies
public static class RateLimitPolicies
{
    public static readonly RateLimitPolicy PublicGet = new("public-get", 60);           // 60 req/min
    public static readonly RateLimitPolicy UserGet = new("user-get", 200);             // 200 req/min
    public static readonly RateLimitPolicy AdminGet = new("admin-get", 1000);          // 1000 req/min
    public static readonly RateLimitPolicy PublicWrite = new("public-write", 10);      // 10 req/min
    public static readonly RateLimitPolicy UserWrite = new("user-write", 50);          // 50 req/min
}
```

### Middleware Implementation
```csharp
// UnitConverter.Core/Middleware/RateLimitMiddleware.cs
using UnitConverter.Core.Contracts.Interfaces;
using UnitConverter.Core.Contracts.Models;

namespace UnitConverter.Core.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<RateLimitMiddleware> _logger;

    public RateLimitMiddleware(RequestDelegate next, IRateLimiter rateLimiter, ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var policy = DetermineRateLimitPolicy(context);
        
        if (policy != null)
        {
            var clientKey = GetClientKey(context);
            var allowed = await _rateLimiter.IsAllowedAsync(clientKey, policy.RequestsPerMinute);
            
            if (!allowed)
            {
                _logger.LogWarning("Rate limit exceeded for {ClientKey}", clientKey);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.ContentType = "application/json";
                
                await context.Response.WriteAsJsonAsync(new
                {
                    status = 429,
                    title = "Too Many Requests",
                    detail = policy.ErrorMessage,
                    retryAfter = 60
                });
                
                return;
            }
        }
        
        await _next(context);
    }

    private RateLimitPolicy? DetermineRateLimitPolicy(HttpContext context)
    {
        // GET requests: public rate limit
        if (context.Request.Method == HttpMethods.Get)
        {
            var isAuthenticated = context.User?.Identity?.IsAuthenticated ?? false;
            var isAdmin = context.User?.IsInRole("Admin") ?? false;
            
            return isAdmin 
                ? RateLimitPolicies.AdminGet
                : isAuthenticated
                    ? RateLimitPolicies.UserGet
                    : RateLimitPolicies.PublicGet;
        }
        
        // POST/PUT/DELETE: write rate limits
        if (context.Request.Method is HttpMethods.Post or HttpMethods.Put or HttpMethods.Delete)
        {
            var isAuthenticated = context.User?.Identity?.IsAuthenticated ?? false;
            
            return isAuthenticated
                ? RateLimitPolicies.UserWrite
                : RateLimitPolicies.PublicWrite;
        }
        
        return null;
    }

    private string GetClientKey(HttpContext context)
    {
        // Use user ID if authenticated, otherwise IP address
        var userId = context.User?.FindFirst("sub")?.Value;
        
        if (!string.IsNullOrEmpty(userId))
            return $"user:{userId}";
        
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ipAddress}";
    }
}

// Extension for adding rate limit middleware
public static class RateLimitMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimit(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RateLimitMiddleware>();
    }
}
```

---

## API Controller with Paging & Rate Limiting

```csharp
// UnitConverter.Auth.API/Controllers/UsersController.cs
using UnitConverter.Core.Contracts.Models;
using UnitConverter.Auth.Domain.Repositories;

namespace UnitConverter.Auth.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserRepository userRepository, ILogger<UsersController> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of users (Admin only).
    /// Rate limited: 1000 requests/min for admins, 200 requests/min for users.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Employee")]
    [ProducesResponseType(typeof(PagedUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] bool descending = true)
    {
        var request = new PagedRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SortBy = sortBy,
            Descending = descending
        };
        
        request.Validate();

        _logger.LogInformation("Fetching users: Page {PageNumber}, Size {PageSize}, Sort {SortBy}", 
            request.PageNumber, request.PageSize, request.SortBy);

        var pagedUsers = await _userRepository.GetPagedAsync(request);

        var response = new PagedUserResponse
        {
            PageNumber = pagedUsers.PageNumber,
            PageSize = pagedUsers.PageSize,
            TotalCount = pagedUsers.TotalCount,
            TotalPages = pagedUsers.TotalPages,
            HasPreviousPage = pagedUsers.HasPreviousPage,
            HasNextPage = pagedUsers.HasNextPage,
            Items = pagedUsers.Items.Select(u => new UserDto
            {
                Id = u.Id.Value,
                Email = u.Email.Value,
                FirstName = u.FirstName,
                LastName = u.LastName,
                CreatedAt = u.CreatedAt
            }).ToList()
        };

        return Ok(response);
    }
}

public sealed class PagedUserResponse
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
    public List<UserDto> Items { get; set; } = new();
}
```

---

## Master Service Registration (Auth.API)

```csharp
// UnitConverter.Auth.API/Extensions/ServiceCollectionExtensions.cs
using UnitConverter.Core.Extensions;
using UnitConverter.Common.Extensions;
using UnitConverter.Auth.Domain.Extensions;
using UnitConverter.Auth.Application.Extensions;
using UnitConverter.Auth.Infrastructure.Extensions;

namespace UnitConverter.Auth.API.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Master registration for Auth Service.
    /// Registers all layers and dependencies in correct order.
    /// Call this ONCE in Program.cs.
    /// </summary>
    public static IServiceCollection AddAuthService(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // 1. Core implementations (Rate limiting, data protection, etc.)
        services.AddCore();
        
        // 2. Common implementations (JWT, password hashing)
        services.AddCommon(configuration);
        
        // 3. Auth-specific layers (repositories, domain logic)
        services.AddAuthDomain()
                .AddAuthApplication()
                .AddAuthInfrastructure(configuration);
        
        // 4. API services
        services.AddControllers();
        services.AddOpenApi();
        services.AddScalarApiReference();
        services.AddHealthChecks();
        services.AddHttpContextAccessor();
        
        return services;
    }
}
```

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// ✅ Single line registers ENTIRE Auth Service with all layers, paging, and rate limiting
builder.Services.AddAuthService(builder.Configuration);

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseScalarApiReference();
    app.UseDeveloperExceptionPage();
}

// ✅ Add rate limiting middleware
app.UseRateLimit();

app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();

await app.RunAsync();
```

---

## Project Dependencies (Correct Flow)

```
✅ UnitConverter.Auth.API
  ↓ references
✅ UnitConverter.Auth.Infrastructure (EF Core implementations)
  ↓ references
✅ UnitConverter.Auth.Domain (Repository implementations)
  ↓ references
✅ UnitConverter.Auth.Application (Handlers, validators)
  ↓ references
✅ UnitConverter.Common (JWT, Password implementations)
  ↓ references
✅ UnitConverter.Core (Rate limiter, data protection)
  ↓ references
✅ UnitConverter.Domain.Contracts (Domain interfaces)
  ↓ references
✅ UnitConverter.Core.Contracts (Core interfaces, paging models)
```

**Key Principle:** Contracts have NO dependencies except System; implementations depend on contracts.

---

## Implementation Checklist for Refactoring

- [ ] Create `UnitConverter.Core.Contracts` project
  - [ ] Add IPagedList, PagedRequest, PagedResult, IRateLimiter
  - [ ] NO ServiceCollectionExtensions

- [ ] Create `UnitConverter.Domain.Contracts` project
  - [ ] Add all domain interfaces (ITokenGenerator, IPasswordHasher, etc.)
  - [ ] NO ServiceCollectionExtensions

- [ ] Refactor `UnitConverter.Core`
  - [ ] Add RateLimiter implementation
  - [ ] Add RateLimitMiddleware
  - [ ] Add ServiceCollectionExtensions.cs (AddCore method)

- [ ] Refactor `UnitConverter.Common`
  - [ ] Add ServiceCollectionExtensions.cs (AddCommon method)

- [ ] Refactor `UnitConverter.Auth.Domain`
  - [ ] Add GetPagedAsync to repositories
  - [ ] Add ServiceCollectionExtensions.cs (AddAuthDomain method)

- [ ] Refactor `UnitConverter.Auth.Application`
  - [ ] Add ServiceCollectionExtensions.cs (AddAuthApplication method)

- [ ] Refactor `UnitConverter.Auth.Infrastructure`
  - [ ] Add ServiceCollectionExtensions.cs (AddAuthInfrastructure method)

- [ ] Refactor `UnitConverter.Auth.API`
  - [ ] Update Program.cs to use master AddAuthService
  - [ ] Add rate limit middleware
  - [ ] Add paging to GET endpoints

- [ ] Repeat for Catalog and Conversion services

