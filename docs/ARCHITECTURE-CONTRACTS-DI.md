# Enhanced Microservices Architecture: Contracts + Implementations with DI

This document redesigns the current Auth Service structure to follow a **Contract-Based Architecture** with shared contracts and layer-specific implementations, including centralized dependency injection.

---

## Architecture Overview

### Current State (Single Project)
```
src/UnitConverter.Auth/
├── Core/Domain/
├── Common/Interfaces/
├── Application/
└── API/
```

### Enhanced State (Distributed Contracts + Implementations)
```
src/
├── UnitConverter.Core.Contracts/           # ⭐ Shared core contracts (domain-agnostic)
│   ├── Interfaces/
│   │   ├── IRepository.cs
│   │   ├── IUnitOfWork.cs
│   │   ├── ILogger.cs
│   │   └── IDataProtector.cs
│   └── ServiceCollectionExtensions.cs      # Empty placeholder for future cross-cutting
│
├── UnitConverter.Domain.Contracts/         # ⭐ Shared domain contracts
│   ├── Interfaces/
│   │   ├── ITokenGenerator.cs
│   │   ├── IPasswordHasher.cs
│   │   └── IAuthenticationService.cs
│   └── ServiceCollectionExtensions.cs      # Register domain contracts
│
├── UnitConverter.Auth/                     # Auth microservice
│   ├── Core/                               # Core layer implementations
│   │   ├── Domain/                         # Domain models
│   │   │   ├── Entities/
│   │   │   ├── ValueObjects/
│   │   │   └── Exceptions/
│   │   └── ServiceCollectionExtensions.cs  # Register Core layer (domain, entities)
│   │
│   ├── Common/                             # Common implementations
│   │   ├── Services/
│   │   │   ├── JwtTokenService.cs          # Implements ITokenGenerator
│   │   │   ├── PasswordService.cs          # Implements IPasswordHasher
│   │   │   └── DataProtectionService.cs    # Implements IDataProtector
│   │   └── ServiceCollectionExtensions.cs  # Register Common implementations
│   │
│   ├── Domain/                             # Domain-specific implementations
│   │   ├── Repositories/                   # Implements IRepository
│   │   ├── Services/
│   │   └── ServiceCollectionExtensions.cs  # Register Domain implementations
│   │
│   ├── Application/                        # Use cases, commands, handlers
│   │   ├── Commands/
│   │   ├── Handlers/
│   │   ├── Validators/
│   │   └── ServiceCollectionExtensions.cs  # Register Application layer
│   │
│   ├── Infrastructure/                     # EF Core, data access
│   │   ├── Data/
│   │   ├── Repositories/
│   │   └── ServiceCollectionExtensions.cs  # Register Infrastructure
│   │
│   ├── API/                                # ASP.NET Core
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   └── Extensions/
│   │       ├── ServiceCollectionExtensions.cs  # Master registration (calls all layers)
│   │       └── MiddlewareExtensions.cs
│   │
│   └── UnitConverter.Auth.csproj
│
└── (Repeat for UnitConverter.Catalog, UnitConverter.Conversion)
```

---

## DI Registration Pattern

### 1. Layer-Specific ServiceCollectionExtensions

Each layer has its own registration method, callable independently.

#### **UnitConverter.Core.Contracts/**
```csharp
namespace UnitConverter.Core.Contracts.Extensions;

public static class CoreContractsServiceCollectionExtensions
{
    /// <summary>
    /// Registers core contracts/interfaces (domain-agnostic abstractions).
    /// Call this once per service bus to ensure contracts are available.
    /// </summary>
    public static IServiceCollection AddCoreContracts(this IServiceCollection services)
    {
        // No implementations registered here - only contract references
        // This ensures all services can depend on core abstractions
        
        return services;
    }
}
```

#### **UnitConverter.Domain.Contracts/**
```csharp
namespace UnitConverter.Domain.Contracts.Extensions;

public static class DomainContractsServiceCollectionExtensions
{
    /// <summary>
    /// Registers domain-specific contracts/interfaces.
    /// Call after AddCoreContracts.
    /// </summary>
    public static IServiceCollection AddDomainContracts(this IServiceCollection services)
    {
        // Register only contract definitions - no implementations
        // Services implementing these contracts registered in specific layers
        
        return services;
    }
}
```

#### **UnitConverter.Auth/Core/**
```csharp
namespace UnitConverter.Auth.Core.Extensions;

public static class CoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers Auth Service Core layer (domain models, entities, exceptions).
    /// No external dependencies - pure domain logic.
    /// Call before Application and Infrastructure layers.
    /// </summary>
    public static IServiceCollection AddAuthCore(this IServiceCollection services)
    {
        // Register domain services if any (e.g., domain event handlers)
        // Core layer is mostly domain models - no DI registrations typically needed
        
        // Example: If there were a domain service
        // services.AddScoped<IDomainService, DomainService>();
        
        return services;
    }
}
```

#### **UnitConverter.Auth/Common/**
```csharp
namespace UnitConverter.Auth.Common.Extensions;

public static class CommonServiceCollectionExtensions
{
    /// <summary>
    /// Registers Auth Service Common layer implementations (JWT, Password, Data Protection).
    /// Dependencies: ITokenGenerator, IPasswordHasher, IDataProtector (from contracts).
    /// Call after Core, before Application.
    /// </summary>
    public static IServiceCollection AddAuthCommon(this IServiceCollection services, IConfiguration configuration)
    {
        // Register services that implement domain contracts
        services.AddScoped<ITokenGenerator, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordService>();
        services.AddScoped<IDataProtector, DataProtectionService>();
        
        // Configure options
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        
        return services;
    }
}
```

#### **UnitConverter.Auth/Domain/**
```csharp
namespace UnitConverter.Auth.Domain.Extensions;

public static class DomainServiceCollectionExtensions
{
    /// <summary>
    /// Registers Auth Service Domain layer (repositories, persistence abstractions).
    /// Dependencies: IRepository, IUnitOfWork (from core contracts).
    /// Call after Core and Common, before Application.
    /// </summary>
    public static IServiceCollection AddAuthDomain(this IServiceCollection services)
    {
        // Register repository implementations
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        
        // Register unit of work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        return services;
    }
}
```

#### **UnitConverter.Auth/Application/**
```csharp
namespace UnitConverter.Auth.Application.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registers Auth Service Application layer (commands, handlers, validators, use cases).
    /// Dependencies: ITokenGenerator, IPasswordHasher, IUserRepository, IValidator (from previous layers).
    /// Call after Core, Common, and Domain.
    /// </summary>
    public static IServiceCollection AddAuthApplication(this IServiceCollection services)
    {
        // Register validators
        services.AddValidatorsFromAssemblyContaining<RegisterUserValidator>();
        
        // Register command handlers
        services.AddScoped(typeof(IRequestHandler<,>), typeof(RegisterUserCommandHandler));
        services.AddScoped(typeof(IRequestHandler<,>), typeof(LoginCommandHandler));
        services.AddScoped(typeof(IRequestHandler<,>), typeof(RefreshTokenCommandHandler));
        
        // Register application services
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        
        return services;
    }
}
```

#### **UnitConverter.Auth/Infrastructure/**
```csharp
namespace UnitConverter.Auth.Infrastructure.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers Auth Service Infrastructure layer (EF Core, data access).
    /// Dependencies: DbContext, IRepository (from domain layer).
    /// Call after all other layers.
    /// </summary>
    public static IServiceCollection AddAuthInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register DbContext
        services.AddDbContext<AuthDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("AuthDb"))
        );
        
        // Register repositories with EF Core implementations
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<IRoleRepository, EfRoleRepository>();
        
        // Register migrations service
        services.AddScoped<IMigrationService, EfMigrationService>();
        
        return services;
    }
}
```

#### **UnitConverter.Auth/API/Extensions/**
```csharp
namespace UnitConverter.Auth.API.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Master registration method for Auth Service.
    /// Calls all layer-specific registrations in correct order.
    /// Call this ONCE in Program.cs.
    /// </summary>
    public static IServiceCollection AddAuthService(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Register contracts
        services
            .AddCoreContracts()
            .AddDomainContracts();
        
        // 2. Register layers in dependency order
        services
            .AddAuthCore()
            .AddAuthCommon(configuration)
            .AddAuthDomain()
            .AddAuthApplication()
            .AddAuthInfrastructure(configuration);
        
        // 3. Register middleware/API-specific services
        services.AddControllers();
        services.AddOpenApi();
        services.AddScalarApiReference();
        
        // 4. Add cross-cutting concerns
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddHealthChecks();
        
        return services;
    }
}
```

---

## Usage in Program.cs

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// ✅ Single call registers entire Auth Service with all layers
builder.Services.AddAuthService(builder.Configuration);

// Optionally add other services
builder.Services.AddCatalogService(builder.Configuration);
builder.Services.AddConversionService(builder.Configuration);

// Build and run
var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseScalarApiReference();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();

await app.RunAsync();
```

---

## Dependency Flow (Clean Architecture)

```
API Layer
    ↓ (depends on)
Application Layer (AddAuthApplication)
    ↓ (depends on)
Domain Layer (AddAuthDomain)
    ↓ (depends on)
Common Layer (AddAuthCommon)
    ↓ (depends on)
Core Layer (AddAuthCore)
    ↓ (depends on)
Domain Contracts (AddDomainContracts)
    ↓ (depends on)
Core Contracts (AddCoreContracts)
    ↓ (NO DEPENDENCIES)

✓ Each layer registers only its own implementations
✓ Contracts are shared, implementations are isolated
✓ DI registration is explicit and layer-aware
✓ Easy to test layers in isolation
✓ Easy to replace implementations
```

---

## Benefits of This Architecture

### 1. **Composability**
```csharp
// Use only Auth service
builder.Services.AddAuthService(config);

// Use Auth + specific layers
builder.Services
    .AddCoreContracts()
    .AddAuthCore()
    .AddAuthCommon(config);
```

### 2. **Testability**
Each layer can be tested in isolation by registering only the layers it depends on.

### 3. **Reusability**
`UnitConverter.Core.Contracts` and `UnitConverter.Domain.Contracts` can be shared across all microservices.

### 4. **Explicit Dependencies**
Each ServiceCollectionExtensions clearly states its dependencies via method parameters.

### 5. **Separation of Concerns**
- **Contracts** layer: Abstractions only
- **Core** layer: Domain models
- **Common** layer: Shared implementations
- **Domain** layer: Persistence
- **Application** layer: Use cases
- **Infrastructure** layer: EF Core details
- **API** layer: Controllers, middleware

---

## Migration Path (From Current to Enhanced)

### Phase 1: Create Shared Contract Projects
- [ ] Create `UnitConverter.Core.Contracts`
- [ ] Create `UnitConverter.Domain.Contracts`
- [ ] Move shared interfaces there

### Phase 2: Add ServiceCollectionExtensions to Current Projects
- [ ] Add `AddAuthCore()` extension
- [ ] Add `AddAuthCommon()` extension
- [ ] Add `AddAuthDomain()` extension
- [ ] Add `AddAuthApplication()` extension
- [ ] Add `AddAuthInfrastructure()` extension
- [ ] Add master `AddAuthService()` extension

### Phase 3: Update Program.cs
- [ ] Replace individual service registrations with single `AddAuthService()` call

### Phase 4: Repeat for Other Microservices
- [ ] Apply same pattern to Catalog Service
- [ ] Apply same pattern to Conversion Service

---

## Example: Adding a New Service (Complete Flow)

Suppose we add a **Notification Service** to Auth:

### 1. Define Contract (in UnitConverter.Domain.Contracts)
```csharp
public interface INotificationService
{
    Task SendEmailAsync(string email, string subject, string message);
}
```

### 2. Implement in Common Layer
```csharp
public class EmailNotificationService : INotificationService
{
    private readonly IEmailProvider _emailProvider;
    
    public EmailNotificationService(IEmailProvider emailProvider)
    {
        _emailProvider = emailProvider;
    }
    
    public async Task SendEmailAsync(string email, string subject, string message)
    {
        // Implementation
    }
}
```

### 3. Register in AddAuthCommon()
```csharp
public static IServiceCollection AddAuthCommon(this IServiceCollection services, IConfiguration configuration)
{
    // Existing registrations...
    
    // Add notification service
    services.AddScoped<INotificationService, EmailNotificationService>();
    services.Configure<EmailSettings>(configuration.GetSection("Email"));
    
    return services;
}
```

### 4. Use in Application Layer
```csharp
public class RegisterUserCommandHandler
{
    private readonly INotificationService _notificationService;
    
    public RegisterUserCommandHandler(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }
    
    public async Task<UserResponse> HandleAsync(RegisterUserCommand command)
    {
        // Register user...
        
        // Send welcome email
        await _notificationService.SendEmailAsync(
            command.Email,
            "Welcome!",
            "Your account has been created."
        );
        
        return response;
    }
}
```

---

## Project Dependencies

### Recommended Project References
```
UnitConverter.Auth.API
    ↓ references
UnitConverter.Auth
    ↓ references
UnitConverter.Domain.Contracts
    ↓ references
UnitConverter.Core.Contracts

UnitConverter.Catalog.API
    ↓ references
UnitConverter.Catalog
    ↓ references
UnitConverter.Domain.Contracts
    ↓ references
UnitConverter.Core.Contracts
```

**No circular dependencies** ✓
**Clear dependency hierarchy** ✓
**Shared contracts** ✓

---

## Implementation Checklist

- [ ] Create `UnitConverter.Core.Contracts` project
- [ ] Create `UnitConverter.Domain.Contracts` project
- [ ] Move shared interfaces to contracts
- [ ] Add layer-specific `ServiceCollectionExtensions.cs` to each layer
- [ ] Update `Program.cs` to use master extension
- [ ] Test: Start application, verify DI registration works
- [ ] Test: Manually instantiate Auth service via DI container
- [ ] Apply pattern to Catalog Service
- [ ] Apply pattern to Conversion Service
- [ ] Update documentation

