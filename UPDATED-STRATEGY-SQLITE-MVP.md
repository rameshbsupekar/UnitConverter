# UPDATED STRATEGY: SQLite MVP + Service-Owned Database

**Date**: June 3, 2026  
**Clarification**: Use SQLite for MVP, service owns all database operations

---

## Architecture Decision: SQLite MVP Model

### Database Strategy
- **Database**: SQLite (file-based, local)
- **Location**: `data/converter.db` (in repo root or deployable folder)
- **Ownership**: Auth Service owns all database operations
- **Access Pattern**: ALL other services/apps call Auth Service API (NO direct DB access)
- **Deployment**: Service ships with database file (or creates on first run)

### Benefits
✅ **Simple**: Single service, single database  
✅ **Portable**: SQLite file included in deployment  
✅ **Local-friendly**: Works without any external setup  
✅ **API-first**: Clean boundaries via REST  
✅ **MVP-ready**: Perfect for initial release  
✅ **Scalable later**: Can migrate to SQL Server/PostgreSQL if needed  

---

## Updated Task R5: SQLite Configuration

### Changes from Original Plan

**Before**: LocalDB + SQLite dual config  
**After**: SQLite everywhere (dev + test + production)

### Simplified R5 Implementation

#### 1. Create appsettings.json (Unified)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "AuthDb": "Data Source=data/converter.db;Cache=Shared"
  },
  "Aspire": {
    "EnsureMigrationsApplied": true,
    "CreateDatabaseFolder": true
  }
}
```

#### 2. Update Program.cs
```csharp
// src/UnitConverter.Auth/API/Program.cs

var builder = WebApplicationBuilder.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("AuthDb");

// Ensure data folder exists
var dataFolder = Path.Combine(AppContext.BaseDirectory, "data");
if (!Directory.Exists(dataFolder))
{
    Directory.CreateDirectory(dataFolder);
}

// Register SQLite DbContext
builder.Services.AddDbContext<AuthDbContext>(options =>
{
    options.UseSqlite(connectionString);
    
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging(true);
    }
});

// ... rest of configuration

var app = builder.Build();

// Apply migrations automatically
var ensureMigrations = app.Configuration.GetValue<bool>("Aspire:EnsureMigrationsApplied");
if (ensureMigrations)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
```

#### 3. Update AuthDbContext (SQLite-specific)
```csharp
// src/UnitConverter.Auth/Infrastructure/Data/AuthDbContext.cs

protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    base.OnConfiguring(optionsBuilder);
    
    // SQLite doesn't support decimals with precision, use double instead
    // Or use TEXT for high-precision storage
}

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // Configure indexes for SQLite performance
    modelBuilder.Entity<User>()
        .HasIndex(u => u.Email)
        .IsUnique()
        .HasDatabaseName("IX_User_Email");
}
```

#### 4. Update Migrations for SQLite
- Migrations already created (Task 6)
- Verify: `dotnet ef migrations list` shows InitialCreate
- SQLite supports all basic operations we need

#### 5. Update Tests (Already SQLite!)
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

    // ... rest of fixture
}
```

#### 6. Repository Structure
```
UnitConverter/
├── data/                           # ← SQLite database folder
│   └── converter.db               # Created on first run
├── src/
│   └── UnitConverter.Auth/API/
│       ├── appsettings.json
│       ├── Program.cs
│       └── ...
├── tests/
│   └── UnitConverter.Auth.Tests/
│       └── Fixtures/DatabaseFixture.cs
└── ...
```

---

## Revised Refactoring Plan (R1-R5)

### R1: Create Contracts Projects + Records (1 hour) ✅
No change - still needed

### R2: Convert DTOs to Records (1 hour) ✅
No change - still needed

### R3: Move Middleware to Auth.Api (1.5 hours) ✅
No change - still needed

### R4: Decouple UnitConverter.Core (1 hour) ✅
No change - still needed

### R5: SQLite Configuration (1 hour - SIMPLIFIED)
**Changes**:
- ✅ Single appsettings.json (no dev/prod split)
- ✅ SQLite everywhere (data/converter.db)
- ✅ Auto-migration on startup
- ✅ In-memory for tests (already SQLite)
- ✅ Auto-create data folder
- ✅ 3-5 tests (config, folder creation, migration)

**Total Time**: ~2.5 hours (same as before, just simpler)

---

## Implementation Pattern (Tasks 7-13)

Each handler can assume:
- **Database**: SQLite, always available
- **Migrations**: Applied automatically
- **Ownership**: Service owns all DB operations
- **Access**: Other services use REST API only

```csharp
// Example: LoginHandler
public class LoginCommandHandler : ICommandHandler<LoginCommand, TokenResponse>
{
    private readonly IUserRepository _users;  // ← Service owns the repo
    private readonly ITokenGenerator _tokens;

    public async Task<TokenResponse> Handle(LoginCommand command)
    {
        // Service queries its own database
        var user = await _users.GetByEmailAsync(command.Email);
        
        // ... authentication logic ...
        
        // Service returns API response
        return new TokenResponse(accessToken, refreshToken, expiresIn, issuedAt);
    }
}

// Other services/apps NEVER access DB directly
// They call: POST /api/v1/auth/login
```

---

## Deployment Model

### Development
```bash
cd UnitConverter
dotnet run --project src/UnitConverter.Auth/API
# → Automatically creates data/converter.db
# → Applies migrations
# → Listens on https://localhost:7000
```

### Production (Docker)
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10
WORKDIR /app
COPY . .
EXPOSE 80
ENTRYPOINT ["dotnet", "UnitConverter.Auth.API.dll"]
# → SQLite file persisted via volume mount or config
# → Migrations applied on startup
```

### Volume Mount (Docker Compose)
```yaml
services:
  auth-api:
    build: .
    ports:
      - "7000:80"
    volumes:
      - ./data:/app/data  # ← Persist SQLite file
```

---

## Summary: What Changes

| Item | Before Plan | Updated Plan |
|------|-------------|--------------|
| **Database** | SQL Server + LocalDB + SQLite | SQLite everywhere |
| **Connection** | Multiple configs | Single appsettings.json |
| **File Location** | System-managed | Repo/data folder |
| **Deployment** | Complex setup | Ship with service |
| **Access** | Direct DB access allowed | API-only access |
| **Ownership** | Blurred | Service owns DB |

---

## Architecture: Service as Database Guardian

```
┌─────────────────────────────────────┐
│   UnitConverter Auth Service         │
├─────────────────────────────────────┤
│ REST API Layer                      │
│  • POST /register                   │
│  • POST /login                      │
│  • POST /refresh-token              │
│  • POST /logout                     │
├─────────────────────────────────────┤
│ Application Layer                   │
│  • CommandHandlers                  │
│  • Validators                       │
│  • Domain Logic                     │
├─────────────────────────────────────┤
│ Infrastructure Layer                │
│  • Repositories (IUserRepository)   │
│  • UnitOfWork (Transaction mgmt)    │
│  • EF Core DbContext                │
├─────────────────────────────────────┤
│ SQLite Database                     │
│  • data/converter.db (local file)   │
└─────────────────────────────────────┘
          ↑
   NO Direct Access
```

---

## Key Principles

✅ **Single Responsibility**: Service owns database operations  
✅ **API-First**: All consumers use REST endpoints  
✅ **Portable**: SQLite file travels with service  
✅ **Simple MVP**: No external database required  
✅ **Scalable**: Can migrate DB later without changing API  

---

## Implementation Impact

### Tasks 7-13 (No Changes Required)
- Handlers access DB through repositories (unchanged)
- Tests use in-memory SQLite (unchanged)
- API contracts remain the same
- Everything just works with simplified R5

### Migration Path (Future)
If you want SQL Server later:
1. Change connection string
2. Regenerate migrations
3. API remains unchanged
4. Consumers don't notice

---

## ✅ READY TO PROCEED WITH SIMPLIFIED APPROACH?

This MVP model:
- ✅ Simplifies everything
- ✅ Maintains clean architecture
- ✅ Enables local development without setup
- ✅ Ships easily to production
- ✅ Supports future scaling

**Proceed with refactoring R1-R5 (simplified)?** YES ✅
