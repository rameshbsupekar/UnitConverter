# SQLite MVP Configuration - Developer Notes

**Date**: June 3, 2026  
**Version**: 1.0 - MVP Implementation

---

## Quick Start

### Running Locally (Development)

```bash
# Clone and navigate to project
cd src/UnitConverter.Auth/API

# Run the application
dotnet run

# Output should show:
# ✓ data/ folder created automatically
# ✓ SQLite database at data/converter.db created
# ✓ Migrations applied automatically
# ✓ Server running on https://localhost:7000
```

No external database setup required!

---

## Configuration

### `appsettings.json`

```json
{
  "ConnectionStrings": {
    "AuthDb": "Data Source=data/converter.db;Cache=Shared"
  },
  "Aspire": {
    "EnsureMigrationsApplied": true,
    "CreateDatabaseFolder": true
  }
}
```

**Key Settings:**
- **Data Source**: Relative path `data/converter.db` creates file in deployment root
- **Cache=Shared**: Allows multiple connections to access SQLite file (required for tests)
- **EnsureMigrationsApplied**: Auto-runs migrations on startup
- **CreateDatabaseFolder**: Reserved for future use (currently done in Program.cs)

### `Program.cs`

Key initialization code:

```csharp
// Ensure data folder exists (created once on startup)
var dataFolder = Path.Combine(AppContext.BaseDirectory, "data");
if (!Directory.Exists(dataFolder))
{
    Directory.CreateDirectory(dataFolder);
}

// SQLite configuration for all environments
builder.Services.AddInfrastructureServices(connectionString, useSqlite: true);

// Apply migrations automatically
if (ensureMigrations)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await db.Database.MigrateAsync();
}
```

---

## Repository Structure

```
UnitConverter/
├── data/                           # Auto-created on first run
│   └── converter.db               # SQLite database file (~2-5 MB typical)
│
├── src/
│   └── UnitConverter.Auth/
│       ├── API/
│       │   ├── appsettings.json   # SQLite connection string
│       │   └── Program.cs         # Folder creation + migrations
│       │
│       ├── Infrastructure/
│       │   └── Data/
│       │       └── AuthDbContext.cs  # Indexes, relationships
│       │
│       └── Migrations/
│           ├── 20240603000000_InitialCreate.cs
│           └── __EFMigrationsHistory
│
└── tests/
    └── UnitConverter.Auth.Tests/
        ├── Integration/
        │   ├── DatabaseTests.cs              # In-memory tests
        │   └── SqliteConfigurationTests.cs   # File-based tests
        │
        └── Fixtures/
            ├── DatabaseFixture.cs     # In-memory setup
            └── TestUserFactory.cs     # Test data
```

---

## Database Schema

**Tables** (created by migrations):
- `Users` - User accounts with unique email index
- `Roles` - Role definitions with unique name index  
- `RefreshTokens` - Active refresh tokens with unique JWT ID index
- `TokenBlacklists` - Revoked JWT tokens with TTL index
- `UserRoles` - Many-to-many relationship (composite key)

**Indexes**:
- `IX_Users_Email_Unique` - Ensures email uniqueness
- `IX_Roles_Name_Unique` - Ensures role name uniqueness
- `IX_RefreshTokens_UserId` - Fast lookup by user
- `IX_RefreshTokens_JwtId_Unique` - Ensures JWT ID uniqueness
- `IX_TokenBlacklists_JwtIdHash_Unique` - Ensures no duplicate blacklist entries
- `IX_TokenBlacklists_TokenExpiresAt` - TTL-based cleanup queries

---

## Testing

### Unit Tests

```bash
# All unit tests (in-memory databases)
dotnet test tests/UnitConverter.Auth.Tests

# Specific test class
dotnet test tests/UnitConverter.Auth.Tests --filter "ClassName=DatabaseTests"

# Verbose output
dotnet test tests/UnitConverter.Auth.Tests --verbosity detailed
```

### Integration Tests - SQLite Configuration

```bash
# Run SQLite config tests (file-based, temporary folders)
dotnet test tests/UnitConverter.Auth.Tests --filter "ClassName=SqliteConfigurationTests"

# These tests verify:
✓ Folder creation
✓ File persistence
✓ Migrations
✓ Unique indexes
✓ Cross-context persistence
✓ Cache=Shared behavior
```

### In-Memory Tests

```bash
# Run existing in-memory database tests
dotnet test tests/UnitConverter.Auth.Tests --filter "ClassName=DatabaseTests"

# These use: "Data Source=:memory:"
# Fast, isolated, no file I/O
```

---

## Development Workflow

### 1. First Time Setup
```bash
dotnet build
dotnet run --project src/UnitConverter.Auth/API
# → data/converter.db created automatically
```

### 2. During Development
- Edit code
- `dotnet run` automatically applies any pending migrations
- Database state is preserved between runs

### 3. Reset Database
```bash
# Stop the app
# Delete: data/converter.db
# Run: dotnet run
# → Fresh database created
```

### 4. Database Migrations

```bash
# Create new migration after schema changes
dotnet ef migrations add DescriptiveName --project src/UnitConverter.Auth

# List all migrations
dotnet ef migrations list --project src/UnitConverter.Auth

# Apply migrations manually (optional - auto-applied on startup)
dotnet ef database update --project src/UnitConverter.Auth
```

---

## Production Deployment

### Docker (Compose)

```yaml
version: '3.8'

services:
  auth-api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "7000:80"
    volumes:
      - ./data:/app/data        # Persist SQLite file
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__AuthDb=Data Source=/app/data/converter.db;Cache=Shared
```

### Docker (Multistage Build)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /build
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=builder /app .
EXPOSE 80
ENTRYPOINT ["dotnet", "UnitConverter.Auth.API.dll"]
```

**Volume Mount for Data Persistence:**
```bash
docker run -v ./data:/app/data -p 7000:80 auth-api
```

### Kubernetes

```yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: auth-db-storage
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 1Gi
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: auth-api
spec:
  replicas: 1
  selector:
    matchLabels:
      app: auth-api
  template:
    metadata:
      labels:
        app: auth-api
    spec:
      containers:
      - name: auth-api
        image: auth-api:latest
        ports:
        - containerPort: 80
        volumeMounts:
        - name: auth-db
          mountPath: /app/data
      volumes:
      - name: auth-db
        persistentVolumeClaim:
          claimName: auth-db-storage
```

---

## SQLite Performance Notes

### Good Practices
✅ **Indexes**: Already configured for Users.Email, Roles.Name, RefreshTokens.JwtId  
✅ **Connection Pooling**: EF Core handles automatically  
✅ **Synchronous Operations**: Most operations are read-only (fast)  
✅ **Cache=Shared**: Allows concurrent read access  

### Limitations (Design Constraints)
⚠️ **Single Writer**: SQLite serializes writes (fine for MVP)  
⚠️ **File Size**: ~50-100 MB typical for 100k+ users (still small)  
⚠️ **Concurrent Writes**: Consider scaling if >100 simultaneous writes/sec  

### When to Migrate to SQL Server/PostgreSQL

Scale up if:
- > 10k concurrent users
- > 1GB database size
- > 100 writes/second sustained

Migration path:
1. Change connection string: `Server=...` (SQL Server)
2. Regenerate migrations: `dotnet ef migrations add SqlServerMigration`
3. Update `useSqlite` parameter to `false`
4. No API changes required!

---

## Troubleshooting

### Issue: "data/converter.db is locked"

**Cause**: Multiple processes accessing database simultaneously without `Cache=Shared`

**Solution**: Ensure connection string includes `Cache=Shared`
```json
"AuthDb": "Data Source=data/converter.db;Cache=Shared"
```

### Issue: "Migrations not applied"

**Cause**: `EnsureMigrationsApplied` set to false

**Solution**: Check `appsettings.json`:
```json
"Aspire": {
  "EnsureMigrationsApplied": true
}
```

### Issue: "Data folder not created"

**Cause**: Program.cs doesn't have folder creation code, or path is wrong

**Solution**: Verify `Program.cs` includes:
```csharp
var dataFolder = Path.Combine(AppContext.BaseDirectory, "data");
if (!Directory.Exists(dataFolder))
{
    Directory.CreateDirectory(dataFolder);
}
```

### Issue: "File permissions denied"

**Cause**: SQLite file lacks write permissions on Linux/Docker

**Solution**: 
```bash
chmod 666 data/converter.db
docker run --user 1000:1000 ...  # Use non-root user
```

---

## Security Notes

### SQLite Considerations
- ✅ File-based encryption available (SQLCipher, but requires additional setup)
- ✅ Credentials not stored in SQLite (JWT tokens used for API auth)
- ⚠️ Database file should be protected like any sensitive data
- ⚠️ Backups should be encrypted in transit

### Best Practices
1. **Database File Protection**:
   - Docker: Use volumes with correct ownership
   - Linux: `chmod 600 data/converter.db`
   - Windows: NTFS permissions restrict to app identity

2. **Connection String Security**:
   - Don't commit passwords in `appsettings.json`
   - Use Azure Key Vault / AWS Secrets in production
   - Environment variables override for sensitive settings

3. **Migration Path**:
   - SQLite → SQL Server: No security reduction (add encryption at SQL Server level)
   - Always use connection pooling and prepared statements (EF Core does this)

---

## Monitoring & Maintenance

### Database Size
```bash
# Linux
du -h data/converter.db

# Windows PowerShell
(Get-Item "data/converter.db").Length / 1MB
```

### Backup Strategy
```bash
# Simple file copy (stop app first, or use readonly copy)
cp data/converter.db data/converter.db.backup.$(date +%Y%m%d)

# Docker volume backup
docker run --rm -v auth-db-storage:/data -v $(pwd):/backup \
  busybox tar czf /backup/backup.tar.gz -C /data .
```

### Cleanup
```bash
# Remove old refresh tokens (auto-done by application logic)
# Remove old blacklisted tokens (query-based cleanup)

# Manual reset (development only!)
rm -f data/converter.db
dotnet run  # Creates fresh database
```

---

## References

- [SQLite Connection Strings](https://www.connectionstrings.com/sqlite/)
- [Entity Framework Core with SQLite](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/)
- [SQLite Shared Cache](https://www.sqlite.org/sharedcache.html)
- [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)

---

## Summary

✅ **MVP Ready**: Zero external dependencies  
✅ **Developer Friendly**: Auto-setup on first run  
✅ **Production Ready**: Docker/Kubernetes deployable  
✅ **Scalable**: Migration path to SQL Server/PostgreSQL  
✅ **Well-Tested**: 8+ integration tests covering config + operations  

**Start Development**: `dotnet run --project src/UnitConverter.Auth/API`
