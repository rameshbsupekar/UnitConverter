# Microservices Architecture: UnitConverter System
**Date:** June 3, 2026  
**Scope:** Design for extreme-scale, highly-reliable, cloud-native unit conversion system  
**Status:** Design Phase Complete

---

## Executive Summary

Evolve the UnitConverter from a monolithic architecture to a **distributed microservices system** optimized for extreme scale (millions of users), reliability, and idempotency. The system comprises three independently-scalable services (Auth, Catalog, Conversion) communicating via HTTP REST APIs, backed by separate databases and intelligent caching.

**Key Goals:**
- ✅ Support extreme-scale traffic (millions of concurrent users)
- ✅ Maintain strong consistency for writes (unique unit constraints)
- ✅ Enable independent scaling per service based on traffic patterns
- ✅ Provide excellent local developer experience (Docker Compose)
- ✅ Be cloud-native ready (Azure App Service, Kubernetes, Azure Functions)
- ✅ Future-proof for message queue adoption without refactoring
- ✅ Implement CQRS at service and handler levels

---

## Section 1: Service Boundaries & Responsibilities

### Service Architecture Overview

The system is decomposed into three independently-deployable microservices:

#### **1. Auth Service** (User & Token Management)
- **Responsibility:** User lifecycle, authentication, authorization, token issuance/validation
- **Database:** Dedicated SQL Server / SQLite (AuthDb)
  - Tables: Users, Roles, UserRoles, RefreshTokens, TokenBlacklist (revocation)
- **Scale Pattern:** Moderate (rarely becomes bottleneck)
- **API Surface:**
  - User registration, login, logout
  - JWT token refresh (15-30 min expiry, refresh tokens for long-lived sessions)
  - Token validation (internal, used by other services)
  - Password reset, role assignment
- **Token Strategy:**
  - JWT tokens for API authentication (short-lived: 15-30 min)
  - Refresh tokens for session extension (long-lived: 7-30 days)
  - API keys for partner integrations (long-lived, revocable)
  - Token validation cacheable (L2 Redis cache, 2-min TTL)

#### **2. Catalog Service** (Unit Management & Approval Workflow)
- **Responsibility:** Unit CRUD, approval workflow, catalog management
- **Database:** Dedicated SQL Server / SQLite (CatalogDb)
  - Tables: Units, UnitCategories, UnitStatus (Pending/Approved/Rejected), ApprovalHistory, ConversionRules
- **Scale Pattern:** Moderate writes, high reads
- **API Surface:**
  - Submit unit (Employee/Partner, creates Pending entry)
  - Approve/Reject unit (Admin, state transition)
  - Query units (with filters: category, status, owner)
  - Update unit metadata (owner only, if Pending)
  - Delete unit (Admin only, soft delete with audit trail)
- **CQRS:**
  - **Commands:** SubmitUnit, ApproveUnit, RejectUnit, UpdateUnit
  - **Queries:** GetUnits, GetUnitById, GetApprovalQueue

#### **3. Conversion Service** (Primary Business Logic - High Traffic)
- **Responsibility:** Fast, reliable unit conversions at scale
- **Database:** None (reads all data from external sources via cache)
  - Conversion rules derived from Catalog Service (cached)
  - Token validation via Auth Service (cached)
- **Scale Pattern:** **Extreme** (scales horizontally with traffic)
- **API Surface:**
  - Convert value between units (POST /convert, high throughput)
  - Get approved catalog (for client-side caching)
- **Caching Strategy:**
  - L1: In-memory HybridCache (5-min TTL)
  - L2: Redis for cluster-wide caching (optional)
  - Refresh: Triggered by Catalog Service webhook or TTL expiry
- **CQRS:**
  - **Queries:** Convert, GetCatalog
  - **No Commands** (read-only from Conversion Service perspective)

### CQRS Application Pattern

| Layer | Command | Query |
|-------|---------|-------|
| **Auth Service** | Register, Login, RefreshToken, RevokeToken | ValidateToken |
| **Catalog Service** | SubmitUnit, ApproveUnit, RejectUnit, UpdateUnit | GetUnits, GetUnitById, GetApprovalQueue |
| **Conversion Service** | (None) | Convert, GetCatalog |

**Command Flow:** Synchronous (HTTP request/response), writes through Catalog Service  
**Query Flow:** Synchronous (HTTP REST), heavily cached  
**Event Flow:** Future-ready (message queue for audit, notifications)

---

## Section 2: Data Flow & API Contracts

### High-Level Data Flow

```
┌─────────────────────────────────────────────────────────────┐
│                   Client (Browser/Mobile/CLI)                │
└────────────────────────────┬────────────────────────────────┘
                             │ HTTP/REST
         ┌───────────────────┼───────────────────┐
         │                   │                   │
         ▼                   ▼                   ▼
    ┌─────────────┐  ┌──────────────┐  ┌──────────────┐
    │   AUTH      │  │  CONVERSION  │  │   CATALOG    │
    │   SERVICE   │  │   SERVICE    │  │   SERVICE    │
    │             │  │              │  │              │
    │ 5001:80     │  │ 5003:80      │  │ 5002:80      │
    └────┬────────┘  └──────┬───────┘  └──────┬───────┘
         │                  │                  │
         │ ← Validates      │ ← Caches        │ ← Reads
         │   Token (L2)     │   Catalog       │   & Writes
         │                  │   (L1 + L2)     │
    ┌────▼──────┐  ┌────────▼────────┐  ┌─────▼──────┐
    │  AuthDb   │  │  HybridCache    │  │ CatalogDb  │
    │  (Users)  │  │  + Redis (opt)  │  │  (Units)   │
    └───────────┘  └─────────────────┘  └────────────┘
```

### Flow 1: Partner Submits a New Unit

```
1. Partner logs in to Web UI
   POST /auth/login (AuthService)
   → Returns JWT token (exp: 15 min) + refresh token

2. Partner submits new unit via Web UI
   POST /units/submit (CatalogService)
   Headers: Authorization: Bearer {JWT}
   Body: {
     "unitName": "Meter",
     "category": "Length",
     "symbol": "m",
     "conversionFactor": 1.0,
     "idempotencyKey": "<UUID>"
   }

3. CatalogService validates request
   - Validates Bearer token via AuthService (cached 2 min)
   - Checks user has Partner role
   - Validates unitName is unique in category (DB constraint)
   - Checks idempotencyKey not seen before (idempotency table)

4. CatalogService executes command
   - Stores unit with status = Pending
   - Stores idempotency result (for retry safety)
   - Audit log: who submitted, when, what

5. CatalogService response
   201 Created: { unitId, status: "Pending", createdAt, ... }

Note: Conversion Service cache is NOT invalidated (Pending units invisible to conversions)
```

### Flow 2: Admin Approves Unit

```
1. Admin logs in, views approval queue
   GET /units/approval-queue (CatalogService)
   Headers: Authorization: Bearer {JWT}
   → Returns list of Pending units

2. Admin approves unit
   PATCH /units/{unitId}/approve (CatalogService)
   Headers: Authorization: Bearer {JWT}
   Body: { reason: "Verified against NIST standards" }

3. CatalogService validates & executes
   - Validates JWT + Admin role
   - Validates unit exists and status = Pending (state machine)
   - Optimistic concurrency check (ETag from GET)
   - Transitions status: Pending → Approved
   - Audit log: admin, timestamp, reason

4. CatalogService invalidates Conversion Service cache
   - Webhook call: POST http://conversion-service:5003/cache/invalidate
   - Conversion Service drops L1 cache (forces refresh on next request)
   - L2 Redis also invalidated

5. Response
   200 OK: { unitId, status: "Approved", approvedAt, ... }

Later: Conversion Service rebuilds cache from Catalog on first request
```

### Flow 3: User Converts Units (High Frequency, Optimized)

```
1. Client calls conversion API
   POST /convert (ConversionService)
   Headers: Authorization: Bearer {JWT}
   Body: { value: 1000, from: "Meter", to: "Foot", precision: 2 }

2. ConversionService execution (ultra-fast path)
   a) Validate token (check L2 cache, TTL 2 min)
      → Cache hit: <5ms (Redis lookup)
      → Cache miss: HTTP call to AuthService
   
   b) Lookup conversion in L1 cache (HybridCache)
      → In memory: <1ms
      → If missing: Fetch from Catalog Service, cache for 5 min
   
   c) Execute conversion math (C# method, <0.1ms)
      - Linear: result = value * factor
      - Affine: result = (value + offset) * factor
   
   d) Return response

3. Response (from cache)
   200 OK: {
     value: 1000,
     fromUnit: "Meter",
     toUnit: "Foot",
     result: 3280.84,
     precision: 2
   }

Latency: <10ms p99 (with cache hits)
```

### API Contracts

#### **Auth Service**

```
POST /auth/register
  Body: { email, password, firstName, lastName, organizationName }
  Response: 201 { userId, email }

POST /auth/login
  Body: { email, password }
  Response: 200 { accessToken, refreshToken, expiresIn, tokenType }

POST /auth/refresh
  Body: { refreshToken }
  Response: 200 { accessToken, expiresIn }

POST /auth/logout
  Body: { refreshToken }
  Response: 204 (No Content)

POST /auth/revoke
  Body: { token }
  Response: 204

GET /auth/validate (internal only)
  Query: ?token=<JWT>
  Response: 200 { isValid, userId, roles, email }
```

#### **Catalog Service**

```
POST /units/submit
  Body: { unitName, category, symbol, conversionFactor, idempotencyKey }
  Response: 201 { unitId, status: "Pending", createdAt }

GET /units
  Query: ?status=Approved&category=Length
  Response: 200 [ { unitId, unitName, symbol, ... } ]

GET /units/{id}
  Response: 200 { unitId, unitName, status, createdBy, approvedBy, ... }

PATCH /units/{id}/approve
  Body: { reason }
  Response: 200 { unitId, status: "Approved", approvedAt }

PATCH /units/{id}/reject
  Body: { reason }
  Response: 200 { unitId, status: "Rejected", rejectedAt }

GET /units/approval-queue
  Response: 200 [ { unitId, unitName, submittedBy, submittedAt, ... } ]
```

#### **Conversion Service**

```
POST /convert
  Body: { value, from, to, precision }
  Response: 200 { value, fromUnit, toUnit, result, precision }

GET /catalog
  Response: 200 {
    categories: [
      {
        name: "Length",
        units: [ { unitName, symbol, baseFactorToSI } ]
      }
    ]
  }

POST /cache/invalidate (internal webhook from Catalog Service)
  Response: 204
```

---

## Section 3: Caching Strategy (L1 + L2)

### L1 Cache: In-Memory (Per Instance)

**Location:** Conversion Service (each instance has its own cache)  
**Technology:** `HybridCache` / `IMemoryCache` (.NET built-in)  
**Scope:** Approved units catalog (read-only snapshot)

```csharp
// In Conversion Service
var catalog = await cache.GetOrCreateAsync(
    key: "approved-units-catalog-v1",
    factory: async (entry) => {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        return await catalogService.GetApprovedUnitsAsync();
    }
);
```

**Content:**
```json
{
  "categories": [
    {
      "name": "Length",
      "units": [
        { "id": "m", "name": "Meter", "symbol": "m", "factor": 1.0 },
        { "id": "ft", "name": "Foot", "symbol": "ft", "factor": 0.3048 }
      ]
    }
  ],
  "lastUpdated": "2026-06-03T12:30:00Z"
}
```

**TTL:** 5 minutes (balance between freshness and performance)  
**Size:** ~100KB (easily fits in memory)  
**Hit Rate:** 99%+ for conversion requests

### L2 Cache: Distributed (Shared Across Instances)

**Location:** Redis (optional, for distributed scenarios)  
**Technology:** Azure Cache for Redis (or Docker Redis)  
**Scope:** Token validation results

```csharp
// In Conversion Service
var isValid = await redisCache.GetAsync<TokenValidationResult>(
    key: $"token-validation:{tokenHash}",
    factory: async () => await authService.ValidateTokenAsync(token),
    options: new CacheOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2) }
);
```

**Content per Cache Entry:**
```json
{
  "tokenHash": "sha256-...",
  "isValid": true,
  "userId": "user-123",
  "roles": ["Partner", "Employee"],
  "email": "partner@org.com",
  "validatedAt": "2026-06-03T12:30:00Z"
}
```

**TTL:** 2 minutes (shorter than token expiry for security)  
**Purpose:** Reduce calls to Auth Service on every request  
**Trade-off:** Slightly delayed token revocation (2-min max stale read)

### L3 Cache: Browser / Client-Side

**Location:** Client (Web UI / Mobile app)  
**Scope:** Approved catalog (GET /catalog endpoint)

Clients fetch once, cache locally:
```javascript
// Client JS
const catalog = await fetch('/catalog').then(r => r.json());
localStorage.setItem('unit-catalog', JSON.stringify(catalog));
// Use locally for conversions, refresh every 10 min
```

### Cache Invalidation Strategy

#### **Catalog Updates (Push-based)**
```
Admin approves unit
  ↓
CatalogService status: Pending → Approved
  ↓
CatalogService webhook: POST http://conversion-service:5003/cache/invalidate
  ↓
ConversionService clears L1 HybridCache
  ↓
ConversionService Redis.DeleteAsync("approved-units-catalog-v1")
  ↓
Next /convert request rebuilds cache from Catalog
```

#### **Token Revocation (Broadcast)**
```
Admin revokes token
  ↓
AuthService invalidates:
  - L2 Redis: delete token-validation:{tokenHash}
  - TokenBlacklist DB: add token + expiry
  ↓
ConversionService validates token → AuthService
  → Checks TokenBlacklist, returns 401 if found
```

#### **TTL Fallback (Passive)**
Even if webhook fails:
- L1 HybridCache expires after 5 min
- L2 Redis expires after 2 min
- System self-heals via TTL

### Performance Targets

| Operation | With L1 Cache | With L1 + L2 | No Cache |
|-----------|--------------|-------------|----------|
| Convert (identical units) | <1ms | <1ms | ~200ms |
| Convert (different units) | <5ms | <5ms | ~250ms |
| Token validation | <5ms | <5ms | ~50ms |
| Catalog fetch | <10ms | <10ms | ~500ms |

---

## Section 4: Error Handling & Idempotency

### Idempotency Design

All **write operations** (commands) must be idempotent. If a client retries the same request, it receives the same response.

#### **Idempotency Key Pattern**

```csharp
// Client MUST include Idempotency-Key header
POST /units/submit
Headers: 
  Authorization: Bearer <JWT>
  Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000

Body: { unitName: "Meter", category: "Length", ... }
```

#### **Database Design**

```sql
CREATE TABLE IdempotencyResults (
    IdempotencyKey NVARCHAR(36) PRIMARY KEY,
    ServiceName NVARCHAR(100) NOT NULL,
    ClientId NVARCHAR(100) NOT NULL,
    RequestBody NVARCHAR(MAX) NOT NULL,
    ResponseStatus INT NOT NULL,
    ResponseBody NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    INDEX IX_ServiceClient (ServiceName, ClientId)
);

-- Composite unique constraint
UNIQUE (IdempotencyKey, ServiceName, ClientId)
```

#### **Implementation Flow**

```csharp
public async Task<SubmitUnitResponse> SubmitUnitAsync(SubmitUnitCommand cmd, string idempotencyKey)
{
    // 1. Check if we've seen this key before
    var existing = await db.IdempotencyResults
        .FirstOrDefaultAsync(i => i.IdempotencyKey == idempotencyKey 
                                && i.ServiceName == "CatalogService"
                                && i.ClientId == currentUser.Id);
    
    if (existing != null)
    {
        // Return cached response (idempotent)
        return JsonConvert.DeserializeObject<SubmitUnitResponse>(existing.ResponseBody);
    }

    // 2. Process command normally
    var result = await ProcessSubmitUnitAsync(cmd);

    // 3. Store idempotency result
    await db.IdempotencyResults.AddAsync(new IdempotencyResult
    {
        IdempotencyKey = idempotencyKey,
        ServiceName = "CatalogService",
        ClientId = currentUser.Id,
        RequestBody = JsonConvert.SerializeObject(cmd),
        ResponseStatus = 201,
        ResponseBody = JsonConvert.SerializeObject(result),
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddDays(7) // 7-day retention
    });
    await db.SaveChangesAsync();

    return result;
}
```

#### **Unique Constraints (Database Level)**

```sql
-- Unit uniqueness per category
CREATE UNIQUE INDEX IX_Unit_NameCategory 
ON Units(UnitName, UnitCategoryId) 
WHERE DeletedAt IS NULL;  -- Soft delete safe

-- Prevents duplicate unit submissions
```

#### **Optimistic Concurrency (for Updates)**

```csharp
// PATCH /units/{id}/approve

PATCH /units/123/approve
Headers:
  Authorization: Bearer <JWT>
  If-Match: "eTag-12345"  // ETag from previous GET

// CatalogService checks ETag matches current version
// If not, returns 412 Precondition Failed (someone else modified)
```

### Error Responses (RFC 7807 Problem Details)

All error responses follow **RFC 7807 Problem Details** format:

```json
{
  "type": "https://unitconverter.example.com/errors/validation-failed",
  "title": "Validation Failed",
  "status": 400,
  "detail": "Unit name 'Meter' already exists in category 'Length'",
  "instance": "/units/submit",
  "traceId": "0HN1GH7F39F01:00000001",
  "correlationId": "corr-12345-67890",
  "timestamp": "2026-06-03T12:30:45Z"
}
```

#### **Common Error Scenarios**

| Scenario | Status | Type | Detail |
|----------|--------|------|--------|
| Duplicate unit name | 400 | `validation-failed` | Unit already exists in category |
| Invalid unit category | 400 | `validation-failed` | Category not found or invalid |
| Invalid token | 401 | `unauthorized` | Token expired or invalid |
| Insufficient permissions | 403 | `forbidden` | User does not have Admin role |
| Unit not found | 404 | `not-found` | Unit with ID 'xyz' not found |
| Concurrent modification | 409 | `conflict` | Unit was modified; retry with latest version |
| Missing idempotency key | 400 | `missing-header` | Idempotency-Key header is required |
| Service unavailable | 503 | `service-unavailable` | Catalog Service temporarily unavailable |

#### **Retry Logic (Client-side)**

```csharp
// Client implements exponential backoff for transient errors
var policy = Policy
    .Handle<HttpRequestException>()
    .Or<TimeoutException>()
    .OrResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500)
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => 
            TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100),
        onRetry: (outcome, timespan, attempts, context) =>
            logger.LogWarning($"Retry attempt {attempts} after {timespan.TotalMilliseconds}ms")
    );

var response = await policy.ExecuteAsync(async () =>
    await client.PostAsJsonAsync("/units/submit", command)
);
```

---

## Section 5: Local Development Setup (Docker Compose)

### Prerequisites
- Docker Desktop (includes Docker & Docker Compose)
- .NET 10 SDK
- Git
- 8 GB RAM (4 GB minimum for 3 services + 2 DBs)

### Docker Compose Configuration

**File:** `docker-compose.yml` at repository root

```yaml
version: '3.8'

services:
  # =================== AUTH SERVICE ===================
  auth-service:
    build:
      context: .
      dockerfile: src/UnitConverter.Auth/Dockerfile
    container_name: uc-auth-service
    ports:
      - "5001:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__AuthDb=Server=auth-db;Database=AuthDb;User Id=sa;Password=SecurePassword123!;Encrypt=false;
      - JwtSettings__Secret=dev-secret-key-min-32-chars-required-for-hs256
      - JwtSettings__Issuer=http://localhost:5001
      - JwtSettings__Audience=unitconverter-api
      - JwtSettings__AccessTokenExpiryMinutes=15
      - JwtSettings__RefreshTokenExpiryDays=7
    depends_on:
      auth-db:
        condition: service_healthy
    networks:
      - uc-network
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:80/health"]
      interval: 10s
      timeout: 3s
      retries: 3

  auth-db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: uc-auth-db
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=SecurePassword123!
    ports:
      - "1433:1433"
    networks:
      - uc-network
    healthcheck:
      test: /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P SecurePassword123! -Q "SELECT 1" || exit 1
      interval: 10s
      timeout: 3s
      retries: 3
    volumes:
      - auth-db-data:/var/opt/mssql/data

  # =================== CATALOG SERVICE ===================
  catalog-service:
    build:
      context: .
      dockerfile: src/UnitConverter.Catalog/Dockerfile
    container_name: uc-catalog-service
    ports:
      - "5002:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__CatalogDb=Server=catalog-db;Database=CatalogDb;User Id=sa;Password=SecurePassword123!;Encrypt=false;
      - Services__AuthServiceUrl=http://auth-service:80
      - Logging__LogLevel__Default=Information
    depends_on:
      catalog-db:
        condition: service_healthy
      auth-service:
        condition: service_healthy
    networks:
      - uc-network
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:80/health"]
      interval: 10s
      timeout: 3s
      retries: 3

  catalog-db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: uc-catalog-db
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=SecurePassword123!
    ports:
      - "1434:1433"
    networks:
      - uc-network
    healthcheck:
      test: /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P SecurePassword123! -Q "SELECT 1" || exit 1
      interval: 10s
      timeout: 3s
      retries: 3
    volumes:
      - catalog-db-data:/var/opt/mssql/data

  # =================== CONVERSION SERVICE ===================
  conversion-service:
    build:
      context: .
      dockerfile: src/UnitConverter.Conversion/Dockerfile
    container_name: uc-conversion-service
    ports:
      - "5003:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - Services__AuthServiceUrl=http://auth-service:80
      - Services__CatalogServiceUrl=http://catalog-service:80
      - Caching__HybridCacheTtlMinutes=5
      - Caching__EnableRedis=false  # Set to true if Redis enabled
      - Logging__LogLevel__Default=Information
    depends_on:
      - auth-service
      - catalog-service
      - redis  # Remove if not using Redis L2 cache
    networks:
      - uc-network
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:80/health"]
      interval: 10s
      timeout: 3s
      retries: 3

  # =================== REDIS (Optional L2 Cache) ===================
  redis:
    image: redis:7-alpine
    container_name: uc-redis
    ports:
      - "6379:6379"
    networks:
      - uc-network
    command: redis-server --appendonly yes
    volumes:
      - redis-data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 3s
      retries: 3

networks:
  uc-network:
    driver: bridge

volumes:
  auth-db-data:
  catalog-db-data:
  redis-data:
```

### Development Workflow

```bash
# 1. Clone repository
git clone <repo>
cd UnitConverter

# 2. Start all services
docker-compose up -d

# 3. Wait for services to be healthy
docker-compose ps
# All should show "healthy" status

# 4. Initialize databases (run migrations)
dotnet run --project src/UnitConverter.Auth -- migrate
dotnet run --project src/UnitConverter.Catalog -- migrate

# 5. Seed test data (optional)
dotnet run --project src/UnitConverter.Catalog -- seed-data

# 6. Run tests
dotnet test

# 7. Run application
dotnet run --project src/UnitConverter.Conversion

# 8. Access services
Auth Service:       http://localhost:5001
Catalog Service:    http://localhost:5002
Conversion Service: http://localhost:5003
API Documentation:  http://localhost:5003/docs (Scalar)
```

### Local Development Commands

```bash
# View logs from all services
docker-compose logs -f

# View logs from specific service
docker-compose logs -f conversion-service

# Stop all services
docker-compose down

# Rebuild images (after code changes)
docker-compose build

# Restart single service
docker-compose restart conversion-service

# Execute migration in Auth Service container
docker-compose exec auth-service dotnet ef database update

# Access SQL Server in container
docker-compose exec auth-db \
  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P SecurePassword123! -Q "SELECT * FROM Users"
```

---

## Section 6: Observability & Performance Benchmarks

### Distributed Tracing (OpenTelemetry)

**Stack:** OpenTelemetry + Jaeger (local) / Application Insights (Azure)

```csharp
// Program.cs (each service)
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("UnitConverter.Conversion"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation()
            .AddJaegerExporter(options =>
            {
                options.AgentHost = "localhost";
                options.AgentPort = 6831;
            });
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter();
    });
```

**Trace Example:**
```
POST /convert
├─ [Conversion Service] Validate Bearer token
│  └─ → HTTP call to Auth Service
│     └─ [Auth Service] Token lookup (cache hit, <5ms)
├─ [Conversion Service] Lookup units in cache
│  └─ HybridCache hit (in-memory, <1ms)
├─ [Conversion Service] Execute conversion
│  └─ Linear conversion math (<0.1ms)
└─ Response sent (total: <10ms)

Trace ID: 0HN1GH7F39F01
```

### Performance Benchmarks & Targets

| Operation | Target p99 | Success Criteria |
|-----------|-----------|-----------------|
| Convert (cached) | <10ms | 99.9% success |
| Convert (cache miss) | <50ms | Automatic recovery |
| Token validation (cached) | <5ms | Minimal Auth Service load |
| Unit submission | <200ms | Strong consistency |
| Unit approval | <300ms | State transition safety |
| Catalog fetch | <100ms | Cache refresh |

### Load Testing (NBomber)

```csharp
// tests/UnitConverter.Load.Tests/ConversionLoadTests.cs
[Test]
public void LoadTest_Conversions_ExtremeScale()
{
    var scenario = Scenario.Create("extreme-scale", async context =>
    {
        var response = await httpClient.PostAsJsonAsync("/convert", new
        {
            value = 1000,
            from = "Meter",
            to = "Foot"
        });

        return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
    })
    .WithLoadSimulations(
        // Phase 1: Ramp up to 10K concurrent users over 30s
        Simulation.RampUp(concurrentCopies: 10000, timeToReach: TimeSpan.FromSeconds(30)),
        // Phase 2: Hold at 10K for 60s
        Simulation.ConstantThroughput(copies: 10000, during: TimeSpan.FromSeconds(60)),
        // Phase 3: Ramp down over 30s
        Simulation.RampDown(concurrentCopies: 0, timeToReach: TimeSpan.FromSeconds(30))
    );

    NBomberRunner
        .RegisterScenarios(scenario)
        .RunTest();
    
    // Assertions
    var stats = NBomberRunner.GetScenarioStats("extreme-scale");
    Assert.That(stats.RPS, Is.GreaterThan(100000)); // >100K req/sec
    Assert.That(stats.FailCount, Is.LessThan(stats.OkCount * 0.01)); // <1% error rate
    Assert.That(stats.Latency.Percentile99, Is.LessThan(50)); // p99 <50ms
}
```

### Micro-Benchmarks (BenchmarkDotNet)

```csharp
// perf/UnitConverter.Benchmarks/ConversionBenchmarks.cs
[MemoryDiagnoser]
public class ConversionBenchmarks
{
    private ConversionService _service;

    [GlobalSetup]
    public void Setup()
    {
        _service = new ConversionService(/* injected dependencies */);
    }

    [Benchmark]
    public decimal Convert_LinearConversion()
    {
        return _service.Convert(1000, "Meter", "Foot");
    }

    [Benchmark]
    public decimal Convert_AffineConversion()
    {
        return _service.Convert(25, "Celsius", "Fahrenheit");
    }

    [Benchmark]
    public void CacheHit_UnitLookup()
    {
        var units = _service.GetCatalog(); // Should be <1ms with cache
    }
}

// Run: dotnet run -c Release --project perf/UnitConverter.Benchmarks/
// Output: BenchmarkDotNet reports throughput, allocations, latency
```

---

## Section 7: Azure Deployment Path

### Local → Azure Migration Strategy

| Stage | Infrastructure | Services | Database | Cache |
|-------|---|---|---|---|
| **Development** | Docker Desktop | 3 containers | SQL Server containers | Redis container |
| **Staging** | Azure Container Instances | 3 ACIs | Azure SQL Database | Azure Cache for Redis |
| **Production** | Azure App Service / AKS | 3+ instances (auto-scale) | Azure SQL Database (geo-replicated) | Azure Cache for Redis |

### Azure Deployment Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Azure Front Door                      │
│              (Global load balancing, DDoS)               │
└─────────────────────────────────────────────────────────┘
                            │
         ┌──────────────────┼──────────────────┐
         │                  │                  │
    ┌────▼────┐        ┌────▼────┐       ┌───▼────┐
    │Auth App │        │Catalog  │       │Convert │
    │Service  │        │Service  │       │Service │
    │         │        │         │       │        │
    │(2-10    │        │(2-5     │       │(5-50+  │
    │instances)│        │instances)│       │instances)
    └────┬────┘        └────┬────┘       └────┬───┘
         │                  │                  │
    ┌────▼──────────────────▼──────────────────▼────┐
    │        Azure SQL Database (Premium Tier)       │
    │  - Read replicas for reporting                 │
    │  - Geo-replication for DR                      │
    └─────────────────────────────────────────────────┘
         │              │              │
    ┌────▼────┐    ┌───▼────┐    ┌───▼────┐
    │AuthDb   │    │CatalogDb│   │Backup  │
    │(Primary)│    │(Primary)│   │(Geo-RP)│
    └─────────┘    └─────────┘   └────────┘

┌─────────────────────────────────────────────────────────┐
│         Azure Cache for Redis (Premium Tier)            │
│    - L2 token validation cache                          │
│    - Shared across all Conversion Service instances     │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│        Application Insights (Observability)             │
│    - Traces, metrics, logs, performance analytics      │
└─────────────────────────────────────────────────────────┘
```

### Auto-Scaling Rules

**Conversion Service (High-traffic):**
- Scale Out: CPU > 70% OR Memory > 80%
- Scale In: CPU < 30% AND Memory < 50%
- Min instances: 5, Max instances: 50
- Cool-down: 2 minutes

**Catalog Service (Moderate traffic):**
- Scale Out: CPU > 60% OR Requests > 1000/sec
- Scale In: CPU < 40%
- Min instances: 2, Max instances: 10

**Auth Service (Low-traffic, high-value):**
- Scale Out: CPU > 80% OR Response time > 1000ms
- Scale In: CPU < 50%
- Min instances: 2, Max instances: 5

### CI/CD Pipeline (Azure DevOps)

```yaml
# azure-pipelines.yml
trigger:
  - main
  - develop

pool:
  vmImage: 'ubuntu-latest'

stages:
  - stage: Build
    jobs:
      - job: BuildAndTest
        steps:
          - task: UseDotNet@2
            inputs:
              version: '10.0.x'
          - task: DotNetCoreCLI@2
            inputs:
              command: 'build'
              projects: '**/UnitConverter.*.csproj'
          - task: DotNetCoreCLI@2
            inputs:
              command: 'test'
              projects: '**/UnitConverter.*.Tests.csproj'
              arguments: '--collect:"XPlat Code Coverage"'
          - task: PublishCodeCoverageResults@1
            inputs:
              codeCoverageTool: Cobertura

  - stage: Deploy_Staging
    dependsOn: Build
    condition: eq(variables['Build.SourceBranch'], 'refs/heads/develop')
    jobs:
      - deployment: DeployStaging
        environment: staging
        strategy:
          runOnce:
            deploy:
              steps:
                - task: AzureWebApp@1
                  inputs:
                    azureSubscription: '...'
                    appType: 'webAppLinux'
                    appName: 'uc-auth-service-staging'

  - stage: Deploy_Production
    dependsOn: Build
    condition: eq(variables['Build.SourceBranch'], 'refs/heads/main')
    jobs:
      - deployment: DeployProduction
        environment: production
        strategy:
          runOnce:
            deploy:
              steps:
                - task: AzureWebApp@1
                  inputs:
                    azureSubscription: '...'
                    appType: 'webAppLinux'
                    appName: 'uc-auth-service-prod'
```

---

## Section 8: API Documentation (Scalar with OpenAPI 3.1)

### OpenAPI-First, Tool-Agnostic Design

**Principle:** API contract (OpenAPI 3.1) is the source of truth. Multiple documentation UIs can read it.

**Tech Stack:**
- **Spec Format:** OpenAPI 3.1 JSON (auto-generated from code via Swashbuckle)
- **Primary UI:** Scalar (modern, clean, dark mode)
- **Fallback UI:** Swagger UI (familiar, can enable if needed)
- **Endpoints:**
  - Scalar: `http://localhost:5003/docs`
  - OpenAPI JSON: `http://localhost:5003/swagger/v1/swagger.json`
  - Alternative Swagger: `http://localhost:5003/swagger/ui` (optional)

### Implementation in Program.cs

```csharp
// Program.cs (each service)
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Unit Conversion API",
        Version = "v1",
        Description = "High-scale, idempotent unit conversion system",
        Contact = new OpenApiContact { Name = "Engineering Team" },
        License = new OpenApiLicense { Name = "MIT" }
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

app.UseSwagger(options =>
{
    options.RouteTemplate = "/swagger/{documentName}/swagger.json";
});

// Scalar documentation UI (primary)
app.MapScalarApiReference()
    .WithOpenApiUrl("/swagger/v1/swagger.json")
    .WithDocumentTitle("Unit Converter API Documentation")
    .WithEndpoint("/docs");

// Optional: Swagger UI (fallback)
// app.UseSwaggerUI(options =>
// {
//     options.SwaggerEndpoint("/swagger/v1/swagger.json", "Unit Converter API v1");
//     options.RoutePrefix = "swagger/ui";
// });
```

### OpenAPI Spec Attributes (C# Annotations)

```csharp
/// <summary>
/// Convert a value from one unit to another
/// </summary>
/// <remarks>
/// Converts numerical values between different units of measurement.
/// Supports linear conversions (e.g., length) and affine conversions (e.g., temperature).
/// 
/// Example request:
/// POST /convert
/// {
///   "value": 1000,
///   "from": "Meter",
///   "to": "Foot",
///   "precision": 2
/// }
/// </remarks>
[HttpPost("convert")]
[ProducesResponseType(typeof(ConversionResult), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ValidationProblem), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(UnauthorizedProblem), StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> Convert([FromBody] ConvertRequest request)
{
    // Implementation
}
```

### Tool Replacement (Future)

To switch documentation UIs later (e.g., Swagger → ReDoc):

1. Remove Scalar setup
2. Add ReDoc setup (same OpenAPI spec)
3. No API code changes needed

```csharp
// Alternative: ReDoc instead of Scalar
app.UseReDoc(options =>
{
    options.SpecUrl = "/swagger/v1/swagger.json";
    options.RoutePrefix = "docs";
});
```

---

## Section 9: Future Message Queue Integration

### Design Ready for Async Events

Today's REST synchronous design is extensible to message queues without refactoring.

### Event-Driven Path (Future)

**Scenario:** When unit approval happens, trigger side effects asynchronously

```
Admin approves unit
  ↓
CatalogService publishes event:
  Event: "UnitApproved"
  Payload: { unitId, unitName, approvedBy, approvedAt }
  ↓
Message Broker (RabbitMQ / Azure Service Bus)
  ↓
Subscribers:
  - Audit Service (log approval history)
  - Notification Service (email admins)
  - Analytics Service (track approvals)
  - Conversion Service (refresh cache)
```

### Implementation Pattern (Ready Now)

```csharp
// Domain event
public class UnitApprovedEvent : IDomainEvent
{
    public string UnitId { get; set; }
    public string UnitName { get; set; }
    public string ApprovedBy { get; set; }
    public DateTime ApprovedAt { get; set; }
}

// Catalog Service publishes event
public class ApproveUnitHandler : ICommandHandler<ApproveUnitCommand>
{
    public async Task<Result> HandleAsync(ApproveUnitCommand cmd)
    {
        var unit = await _unitRepository.GetByIdAsync(cmd.UnitId);
        unit.Approve(cmd.Reason);
        await _unitRepository.SaveAsync(unit);
        
        // Publish event (infrastructure choice later: REST webhook, message queue, gRPC)
        await _eventPublisher.PublishAsync(new UnitApprovedEvent
        {
            UnitId = unit.Id,
            UnitName = unit.Name,
            ApprovedBy = cmd.ApprovedBy,
            ApprovedAt = DateTime.UtcNow
        });

        return Result.Success();
    }
}

// Event publisher interface (implementation swappable)
public interface IEventPublisher
{
    Task PublishAsync<T>(T domainEvent) where T : IDomainEvent;
}

// Implementation 1 (today): REST webhook
public class RestWebhookPublisher : IEventPublisher
{
    public async Task PublishAsync<T>(T evt) where T : IDomainEvent
    {
        await _httpClient.PostAsJsonAsync(
            "http://conversion-service/cache/invalidate",
            evt
        );
    }
}

// Implementation 2 (future): Message queue
public class RabbitMqPublisher : IEventPublisher
{
    public async Task PublishAsync<T>(T evt) where T : IDomainEvent
    {
        var channel = _connection.CreateModel();
        var json = JsonConvert.SerializeObject(evt);
        var body = Encoding.UTF8.GetBytes(json);
        channel.BasicPublish("unitconverter-events", typeof(T).Name, null, body);
    }
}

// Swap in Startup: services.AddSingleton<IEventPublisher, RabbitMqPublisher>();
```

**No domain code changes needed** — only infrastructure implementation swapped.

---

## Section 10: Security-First Architecture (ASP.NET Core 8)

This section implements security best practices from "Advanced ASP.NET Core 8 Security" comprehensively across all layers: authentication, authorization, data protection, API security, input validation, secure headers, and monitoring.

### 10.1 Authentication & Authorization Strategy

#### **Token-Based Authentication (OAuth 2.0 / OpenID Connect Ready)**

**Auth Service implements:**
```csharp
// Program.cs - Auth Service startup
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = "https://unitconverter.example.com",
            ValidateAudience = true,
            ValidAudiences = new[] { "unitconverter-api", "unitconverter-web" },
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero, // Strict expiry validation
            RequireExpirationTime = true
        };
        
        // Security: Reject tokens without required claims
        options.SecurityTokenValidators.Clear();
        options.SecurityTokenValidators.Add(new CustomJwtSecurityTokenHandler());
    });

builder.Services
    .AddAuthorization(options =>
    {
        // Role-based policies
        options.AddPolicy("AdminOnly", policy => 
            policy.RequireRole("Admin"));
        
        options.AddPolicy("PartnerOrEmployee", policy =>
            policy.RequireRole("Partner", "Employee"));
        
        // Claim-based policies (resource authorization)
        options.AddPolicy("CanApproveUnits", policy =>
            policy.RequireClaim("can_approve_units", "true"));
        
        // Custom policy with minimum auth age
        options.AddPolicy("FreshAuth", policy =>
            policy.AddRequirement(new FreshAuthenticationRequirement(
                maxAgeSeconds: 300 // 5 minutes
            )));
    });
```

**JWT Token Claims (Minimal but Sufficient):**
```json
{
  "sub": "user-123",
  "email": "partner@org.com",
  "roles": ["Partner"],
  "org_id": "org-456",
  "can_approve_units": "false",
  "iat": 1717418400,
  "exp": 1717418900,
  "iss": "https://unitconverter.example.com",
  "aud": "unitconverter-api",
  "jti": "unique-token-id"  // JWT ID for revocation tracking
}
```

**Refresh Token Rotation (Secure):**
```csharp
// Refresh endpoint with rotation
public async Task<RefreshTokenResponse> RefreshAsync(RefreshTokenRequest request)
{
    // 1. Validate refresh token exists in DB
    var storedToken = await _db.RefreshTokens
        .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);
    
    if (storedToken == null || storedToken.ExpiresAt < DateTime.UtcNow)
        throw new SecurityException("Invalid or expired refresh token");
    
    // 2. Check if token is blacklisted (revoked)
    var isBlacklisted = await _db.TokenBlacklist
        .AnyAsync(tb => tb.TokenId == storedToken.TokenId);
    
    if (isBlacklisted)
        throw new SecurityException("Token has been revoked");
    
    // 3. Generate new tokens
    var newAccessToken = GenerateAccessToken(storedToken.UserId);
    var newRefreshToken = GenerateRefreshToken(storedToken.UserId);
    
    // 4. Rotate refresh token (old one invalidated)
    storedToken.Token = newRefreshToken.Token;
    storedToken.ExpiresAt = newRefreshToken.ExpiresAt;
    storedToken.LastRotatedAt = DateTime.UtcNow;
    
    await _db.SaveChangesAsync();
    
    return new RefreshTokenResponse
    {
        AccessToken = newAccessToken,
        RefreshToken = newRefreshToken.Token,
        ExpiresIn = 900 // 15 minutes
    };
}
```

**API Key Authentication (Partner Integration):**
```csharp
// Custom handler for partner API keys
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
            return AuthenticateResult.NoResult();

        var apiKey = apiKeyHeader.ToString();
        
        // 1. Lookup API key securely
        var keyHash = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        var partner = await _db.Partners
            .FirstOrDefaultAsync(p => p.ApiKeyHash == keyHash);
        
        if (partner == null || partner.IsRevoked)
            return AuthenticateResult.Fail("Invalid API key");
        
        // 2. Check key expiry
        if (partner.ApiKeyExpiresAt < DateTime.UtcNow)
            return AuthenticateResult.Fail("API key expired");
        
        // 3. Rate limit per API key
        await _rateLimitService.CheckLimitAsync($"api-key:{partner.Id}");
        
        // 4. Create principal
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, partner.Id.ToString()),
            new Claim("partner_id", partner.Id.ToString()),
            new Claim(ClaimTypes.Role, "Partner")
        };
        
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        
        return AuthenticateResult.Success(
            new AuthenticationTicket(principal, Scheme.Name)
        );
    }
}

// Register in Program.cs
builder.Services.AddAuthentication()
    .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        "ApiKey", options => { });
```

#### **Multi-Factor Authentication (Optional, Future)**
```csharp
// Admin users require MFA via TOTP (Authenticator app)
// Implementation ready but not required for v1
builder.Services.AddAuthentication()
    .AddJwtBearer(options => { /* ... */ })
    .AddTotpAuthenticator();  // Future
```

---

### 10.2 Data Protection & Secrets Management

#### **Encryption at Rest & In Transit**

**TLS/HTTPS Enforcement:**
```csharp
// Program.cs - enforced for all services
builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 443;
});

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365); // 1 year
    options.IncludeSubDomains = true;
    options.Preload = true; // HSTS preload list
});

app.UseHttpsRedirection();
app.UseHsts();
```

**Sensitive Data Encryption (in database):**
```csharp
// Data Protection API (DPAPI / AESSGCM)
public class SensitiveDataProtector
{
    private readonly IDataProtectionProvider _provider;
    
    public string EncryptApiKey(string apiKey)
    {
        var protector = _provider.CreateProtector("UnitConverter.Partners.ApiKeys");
        return protector.Protect(apiKey);
    }
    
    public string DecryptApiKey(string encryptedApiKey)
    {
        var protector = _provider.CreateProtector("UnitConverter.Partners.ApiKeys");
        return protector.Unprotect(encryptedApiKey);
    }
}

// In Database schema
CREATE TABLE Partners (
    Id BIGINT PRIMARY KEY,
    ApiKeyHash VARBINARY(64) NOT NULL,        -- SHA256 hash only
    ApiKeyEncrypted NVARCHAR(MAX) NOT NULL,   -- Encrypted with DPAPI
    IsRevoked BIT DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL,
    ExpiresAt DATETIME2 NOT NULL
);
```

**Azure Key Vault Integration:**
```csharp
// Program.cs - load secrets from Azure Key Vault
var keyVaultUrl = new Uri("https://<vault-name>.vault.azure.net/");
builder.Configuration.AddAzureKeyVault(
    keyVaultUrl,
    new ClientSecretCredential(
        tenantId, 
        clientId, 
        clientSecret
    )
);

// Environment-specific secret rotation
builder.Services.AddKeyRotationService(options =>
{
    options.RotationCheckIntervalMinutes = 60;
    options.RotationPath = "rotated-secrets/";
});
```

#### **Secure Secrets Management (Local Dev)**
```bash
# docker-compose.yml - never hardcode secrets
environment:
  - ConnectionStrings__AuthDb=Server=auth-db;Database=AuthDb;User Id=sa;Password=${SA_PASSWORD};
  - JwtSettings__Secret=${JWT_SECRET}
  - ApiKey__MasterKey=${API_KEY_MASTER}

# .env file (local, NEVER committed)
SA_PASSWORD=RandomSecurePassword123!
JWT_SECRET=dev-secret-key-min-32-chars-required
API_KEY_MASTER=random-master-key-for-key-derivation
```

```yaml
# docker-compose.yml - secrets from file
secrets:
  db_password:
    file: ./secrets/db_password.txt
  jwt_secret:
    file: ./secrets/jwt_secret.txt

services:
  auth-db:
    environment:
      - SA_PASSWORD_FILE=/run/secrets/db_password
```

---

### 10.3 API Security (CORS, CSRF, Rate Limiting, API Versioning)

#### **CORS (Cross-Origin Resource Sharing)**
```csharp
// Program.cs - restrict origins strictly
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy
            .WithOrigins(
                "https://unitconverter.example.com",
                "https://api.unitconverter.example.com"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("X-Pagination-Count", "X-Pagination-PageSize")
            .SetPreflightMaxAge(TimeSpan.FromHours(1));
    });
    
    options.AddPolicy("Development", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000", "http://localhost:5000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var environment = app.Environment.IsProduction() ? "Production" : "Development";
app.UseCors(environment);
```

#### **CSRF Protection**
```csharp
// Program.cs
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-Token";
    options.SuppressXFrameOptionsHeader = false;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// In controllers - for state-changing operations
[HttpPost("units/submit")]
[ValidateAntiforgeryToken]  // Razor Pages / Web UI
public async Task<IActionResult> SubmitUnit([FromBody] SubmitUnitCommand cmd)
{
    // API consumers: CSRF not applicable (stateless token auth)
    // Web UI: CSRF token required in POST
}
```

#### **Rate Limiting (Per User, Per IP, Per API Key)**
```csharp
// Program.cs
builder.Services.AddRateLimiter(options =>
{
    // Fixed window rate limiter
    var defaultPolicy = RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext => 
            httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown",
        factory: partition => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 5
        });
    
    options.GlobalLimiter = defaultPolicy;
    
    // Stricter limit for write operations
    options.AddPolicy("write", context =>
    {
        if (context.Request.Method != HttpMethods.Get)
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown",
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1)
                });
        }
        
        return RateLimitPartition.NoLimiter;
    });
});

app.UseRateLimiter();

// In controller
[HttpPost("units/submit")]
[RateLimitPolicy("write")]
public async Task<IActionResult> SubmitUnit([FromBody] SubmitUnitCommand cmd) { }
```

#### **API Versioning**
```csharp
// Program.cs
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-API-Version")
    );
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Usage in controller
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ConversionController : ControllerBase
{
    [HttpPost("convert")]
    public async Task<IActionResult> Convert([FromBody] ConvertRequest request) { }
}

// v2 with breaking changes
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ConversionControllerV2 : ControllerBase
{
    [HttpPost("convert")]
    public async Task<IActionResult> Convert([FromBody] ConvertRequestV2 request) { }
}
```

---

### 10.4 Input Validation & Output Encoding (OWASP A1/A03)

#### **Server-Side Input Validation**
```csharp
// Domain models with annotations
public class SubmitUnitCommand
{
    [Required(ErrorMessage = "Unit name is required")]
    [StringLength(100, MinimumLength = 1, 
        ErrorMessage = "Unit name must be 1-100 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-_]+$", 
        ErrorMessage = "Unit name contains invalid characters")]
    public string UnitName { get; set; }
    
    [Required]
    [StringLength(50)]
    [RegularExpression(@"^[A-Za-z]{1,3}$", 
        ErrorMessage = "Symbol must be 1-3 letters")]
    public string Symbol { get; set; }
    
    [Range(0.00001, double.MaxValue, 
        ErrorMessage = "Conversion factor must be positive")]
    public decimal ConversionFactor { get; set; }
    
    [Range(0, 10, ErrorMessage = "Precision must be 0-10")]
    public int Precision { get; set; } = 2;
    
    [Required]
    [EmailAddress]
    public string IdempotencyKey { get; set; }  // Actually UUID, but example
}

// Fluent validation (advanced)
public class SubmitUnitCommandValidator : AbstractValidator<SubmitUnitCommand>
{
    public SubmitUnitCommandValidator(IUnitRepository unitRepository)
    {
        RuleFor(x => x.UnitName)
            .NotEmpty()
            .Length(1, 100)
            .Matches(@"^[a-zA-Z0-9\s\-_]+$")
            .Must(name => !unitRepository.ExistsAsync(name).Result)
            .WithMessage("Unit already exists");
        
        RuleFor(x => x.ConversionFactor)
            .GreaterThan(0);
    }
}

// Custom validation middleware
public class RequestValidationMiddleware
{
    private readonly RequestDelegate _next;
    
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.ContentType?.Contains("application/json") == true)
        {
            context.Request.EnableBuffering();
            
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();
            
            // Validate JSON size (prevent large payload attacks)
            if (body.Length > 10 * 1024 * 1024)  // 10MB limit
            {
                context.Response.StatusCode = 413;
                await context.Response.WriteAsJsonAsync(new 
                { 
                    error = "Payload too large" 
                });
                return;
            }
            
            context.Request.Body.Position = 0;
        }
        
        await _next(context);
    }
}
```

#### **Output Encoding (Prevent XSS)**
```csharp
// Razor Pages - automatic HTML encoding
@* In .cshtml files, @ operator automatically encodes *@
<p>@Html.Encode(Model.UnitName)</p>  @* Or just @Model.UnitName *@

// JSON response encoding
public class ConversionResult
{
    // Automatic UTF-8 encoding in JSON serializer
    public decimal Value { get; set; }
    public string FromUnit { get; set; }  // Auto-encoded by JsonSerializer
    public string ToUnit { get; set; }
}

// Content Security Policy (CSP) to prevent XSS
builder.Services.AddCsp(options =>
{
    options.DefaultSources(s => s.Self())
        .ScriptSources(s => s
            .Self()
            .UnsafeInline()  // Only for development; use nonce in production
            .Strict()
        )
        .StyleSources(s => s
            .Self()
            .UnsafeInline()
        )
        .ImageSources(s => s.Self())
        .FontSources(s => s.Self())
        .ConnectSources(s => s.Self());
});
```

#### **SQL Injection Prevention (Entity Framework Core)**
```csharp
// ❌ NEVER use string concatenation
// var query = $"SELECT * FROM Units WHERE UnitName = '{unitName}'";

// ✅ ALWAYS use parameterized queries (EF Core handles this)
var unit = await _db.Units
    .FirstOrDefaultAsync(u => u.UnitName == unitName);

// ✅ Or use raw SQL with parameters
var unit = await _db.Units
    .FromSqlInterpolated($"SELECT * FROM Units WHERE UnitName = {unitName}")
    .FirstOrDefaultAsync();

// ✅ Never trust user input in dynamic LINQ
var sortColumn = request.SortBy;  // Don't use in OrderBy directly
var validSortColumns = new[] { "Name", "Category", "CreatedAt" };
if (!validSortColumns.Contains(sortColumn))
    throw new ArgumentException("Invalid sort column");
```

---

### 10.5 Secure Headers & HTTPS Enforcement

#### **Security Headers (Middleware)**
```csharp
// Program.cs
app.UseMiddleware<SecurityHeadersMiddleware>();

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    
    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Strict-Transport-Security (HSTS)
        context.Response.Headers.Add("Strict-Transport-Security", 
            "max-age=31536000; includeSubDomains; preload");
        
        // Content-Security-Policy (CSP)
        context.Response.Headers.Add("Content-Security-Policy",
            "default-src 'self'; script-src 'self'; style-src 'self'");
        
        // X-Content-Type-Options
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        
        // X-Frame-Options (prevent clickjacking)
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        
        // X-XSS-Protection (legacy, but good for old browsers)
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        
        // Referrer-Policy
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        
        // Remove server version header (information disclosure)
        context.Response.Headers.Remove("Server");
        
        // Remove X-Powered-By (information disclosure)
        context.Response.Headers.Remove("X-Powered-By");
        
        await _next(context);
    }
}
```

#### **Secure Cookie Configuration**
```csharp
// Program.cs
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.Strict;
    options.Secure = CookieSecurePolicy.Always;
    options.HttpOnly = true;
});

// Session configuration
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

app.UseCookiePolicy();
app.UseSession();
```

---

### 10.6 Audit Logging & Security Monitoring

#### **Structured Audit Logging**
```csharp
// Domain events for audit
public class UnitApprovedEvent : ISecurityAuditEvent
{
    public string EventType => "UnitApproved";
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; }
    public string UserRole { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public string ResourceId { get; set; }  // UnitId
    public string Details { get; set; }
    public SecuritySeverity Severity => SecuritySeverity.Info;
}

// Audit logging middleware
public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Log before request
        var startTime = DateTime.UtcNow;
        var correlationId = Guid.NewGuid().ToString();
        context.Items["CorrelationId"] = correlationId;
        
        // Log security-relevant requests
        if (IsSensitiveOperation(context.Request))
        {
            var auditEvent = new AuditLogEntry
            {
                CorrelationId = correlationId,
                Timestamp = startTime,
                Method = context.Request.Method,
                Path = context.Request.Path,
                UserId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers["User-Agent"].ToString(),
                QueryString = context.Request.QueryString.ToString()
            };
            
            _logger.LogInformation("SecurityAudit: {@AuditEvent}", auditEvent);
        }
        
        // Log response
        var statusCode = context.Response.StatusCode;
        var duration = DateTime.UtcNow - startTime;
        
        if (statusCode >= 400)  // Log all errors
        {
            _logger.LogWarning(
                "SecurityEvent: {Method} {Path} returned {StatusCode} in {Duration}ms",
                context.Request.Method, context.Request.Path, statusCode, duration.TotalMilliseconds
            );
        }
        
        await _next(context);
    }
    
    private bool IsSensitiveOperation(HttpRequest request)
    {
        var sensitivePatterns = new[] 
        { 
            "/units/submit", 
            "/units/approve", 
            "/auth/login",
            "/auth/refresh"
        };
        
        return sensitivePatterns.Any(p => 
            request.Path.StartsWithSegments(p)
        );
    }
}
```

#### **Anomaly Detection & Alerting**
```csharp
// Monitor suspicious patterns
public class SecurityAnomalyDetector
{
    private readonly ILogger<SecurityAnomalyDetector> _logger;
    
    public async Task MonitorAsync(IEnumerable<AuditLogEntry> recentEvents)
    {
        // Multiple failed auth attempts from same IP
        var failedAttempts = recentEvents
            .Where(e => e.EventType == "LoginFailed")
            .GroupBy(e => e.IpAddress)
            .Where(g => g.Count() > 5)
            .ToList();
        
        foreach (var group in failedAttempts)
        {
            _logger.LogWarning(
                "SecurityAlert: Possible brute force attack from {IpAddress}. Attempts: {Count}",
                group.Key, group.Count()
            );
            
            // Trigger rate limit increase for this IP
            await _rateLimitService.BlockIpAsync(group.Key, TimeSpan.FromHours(1));
        }
        
        // Unusual approval patterns (admin approves units too quickly)
        var approvals = recentEvents
            .Where(e => e.EventType == "UnitApproved")
            .GroupBy(e => e.UserId)
            .Where(g => g.Count() > 100)  // 100+ approvals in monitoring window
            .ToList();
        
        foreach (var group in approvals)
        {
            _logger.LogWarning(
                "SecurityAlert: Unusual approval activity by admin {UserId}",
                group.Key
            );
        }
    }
}
```

#### **Compliance Logging (GDPR, SOC2)**
```csharp
// Data access logging for compliance
public class ComplianceAuditLogger
{
    public async Task LogDataAccessAsync(string userId, string dataType, string action)
    {
        // Log: User {userId} accessed {dataType} (for GDPR right-to-know)
        var entry = new ComplianceAuditLog
        {
            UserId = userId,
            DataType = dataType,
            Action = action,
            Timestamp = DateTime.UtcNow,
            RetentionDays = 2555  // 7 years for compliance
        };
        
        await _db.ComplianceAuditLogs.AddAsync(entry);
        await _db.SaveChangesAsync();
    }
}
```

---

### 10.7 Client-Side Security (Web UI with Web Components)

#### **Secure Razor Pages + Web Components**

**Razor Pages (ASP.NET Core):**
```csharp
// Pages/Convert.cshtml.cs
public class ConvertModel : PageModel
{
    [BindProperty]
    public ConvertInputModel Input { get; set; }
    
    public async Task<IActionResult> OnPostAsync()
    {
        // 1. Validate model (server-side)
        if (!ModelState.IsValid)
            return Page();
        
        // 2. Validate user is authenticated
        if (!User.Identity.IsAuthenticated)
            return Forbid();
        
        // 3. Call API with Bearer token
        var result = await _conversionService.ConvertAsync(Input);
        
        return new JsonResult(result);
    }
}

// Pages/Convert.cshtml (Razor template)
@page
@model ConvertModel
@{
    ViewData["Title"] = "Convert Units";
}

<form method="post">
    @* CSRF token (for form-based submissions) *@
    <input type="hidden" name="__RequestVerificationToken" value="@Html.AntiForgeryToken()" />
    
    <label for="Value">Value:</label>
    <input type="number" id="Value" name="Input.Value" asp-for="Input.Value" />
    
    <button type="submit">Convert</button>
</form>

@* Web Component for advanced UI *@
<unit-converter-widget 
    api-url="https://api.example.com"
    auth-token="@ViewBag.AuthToken">
</unit-converter-widget>

@section Scripts {
    <script src="~/js/unit-converter-widget.js"></script>
}
```

**Web Component (TypeScript/Lit):**
```typescript
// js/unit-converter-widget.ts
import { LitElement, html } from 'lit';
import { customElement, property } from 'lit/decorators.js';

@customElement('unit-converter-widget')
export class UnitConverterWidget extends LitElement {
    @property() apiUrl: string;
    @property() authToken: string;
    
    private value: number = 0;
    private fromUnit: string = 'Meter';
    private toUnit: string = 'Foot';
    
    protected render() {
        return html`
            <div class="converter-container">
                <input 
                    type="number" 
                    placeholder="Enter value"
                    @input="${this.onInputChange}"
                    @keyup="${this.sanitizeInput}"
                />
                <button @click="${this.convert}">Convert</button>
                <p>${this.formatResult()}</p>
            </div>
        `;
    }
    
    private async convert() {
        // 1. Validate input
        if (!this.value || this.value <= 0) {
            this.showError('Value must be positive');
            return;
        }
        
        // 2. Sanitize before sending (defense in depth)
        const sanitizedFrom = DOMPurify.sanitize(this.fromUnit);
        const sanitizedTo = DOMPurify.sanitize(this.toUnit);
        
        // 3. Call API with Bearer token
        const response = await fetch(`${this.apiUrl}/convert`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${this.authToken}`,
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'  // CSRF mitigation
            },
            body: JSON.stringify({
                value: this.value,
                from: sanitizedFrom,
                to: sanitizedTo
            }),
            credentials: 'include'  // Include cookies for CSRF token
        });
        
        if (!response.ok) {
            if (response.status === 401) {
                // Token expired, redirect to login
                window.location.href = '/auth/login';
                return;
            }
            throw new Error('Conversion failed');
        }
        
        const result = await response.json();
        this.displayResult(result);
    }
    
    private sanitizeInput(e: Event) {
        const input = e.target as HTMLInputElement;
        // Only allow numbers and decimal point
        input.value = input.value.replace(/[^0-9.]/g, '');
    }
    
    private showError(message: string) {
        // XSS protection: use textContent, not innerHTML
        const errorEl = document.createElement('div');
        errorEl.textContent = message;
        errorEl.className = 'error';
        this.renderRoot.appendChild(errorEl);
    }
}
```

---

### 10.8 Security Testing & Automation

#### **Automated Security Scanning**

**SAST (Static Application Security Testing):**
```yaml
# .github/workflows/security.yml
name: Security Scanning

on: [push, pull_request]

jobs:
  sast:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      
      # SonarCloud (code quality + security)
      - name: SonarCloud Scan
        uses: SonarSource/sonarcloud-github-action@master
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
          SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
      
      # Semgrep (pattern-based security)
      - name: Semgrep
        uses: returntocorp/semgrep-action@v1
        with:
          generateSarif: true
      
      # Upload SARIF results
      - name: Upload SARIF
        uses: github/codeql-action/upload-sarif@v1
        with:
          sarif_file: semgrep.sarif
```

**SCA (Software Composition Analysis):**
```yaml
  sca:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      
      # NuGet audit (check for vulnerable packages)
      - name: NuGet Audit
        run: dotnet list package --vulnerable
      
      # OWASP Dependency-Check
      - name: Dependency Check
        uses: dependency-check/Dependency-Check_Action@main
        with:
          path: '.'
          format: 'SARIF'
          args: >
            -l
            --enableExperimental
```

**DAST (Dynamic Application Security Testing):**
```bash
# Local security testing with OWASP ZAP
docker run -v $(pwd):/zap/wrk:rw --rm owasp/zap2docker-stable:latest \
  zap-baseline.py -t http://localhost:5003 \
  -r security_report.html
```

#### **Security Unit Tests**
```csharp
[TestClass]
public class SecurityTests
{
    [TestMethod]
    public void TokenValidation_InvalidToken_ReturnsForbidden()
    {
        // Arrange
        var invalidToken = "invalid.token.here";
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", invalidToken);
        
        // Act
        var response = client.PostAsJsonAsync("/convert", new { value = 100 }).Result;
        
        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    
    [TestMethod]
    public void InputValidation_SqlInjection_ReturnsBadRequest()
    {
        // Arrange
        var maliciousInput = "'; DROP TABLE Units; --";
        
        // Act
        var response = client.PostAsJsonAsync("/units/submit", new 
        { 
            unitName = maliciousInput 
        }).Result;
        
        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [TestMethod]
    public void RateLimiting_ExceedsLimit_ReturnsTooManyRequests()
    {
        // Arrange - make 101 requests (limit is 100/min)
        var tasks = Enumerable.Range(0, 101)
            .Select(_ => client.PostAsJsonAsync("/convert", new { value = 100 }))
            .ToList();
        
        // Act
        Task.WaitAll(tasks.ToArray());
        
        // Assert
        var lastResponse = tasks[100].Result;
        Assert.AreEqual(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
    }
}
```

---

### 10.9 Security Checklist (Per Deployment)

**Before Every Production Deployment:**

- [ ] HTTPS certificate valid and up-to-date (check with SSL Labs)
- [ ] JWT secrets rotated (no hardcoded values)
- [ ] Database credentials in Azure Key Vault (not environment variables)
- [ ] CORS whitelist restricted to production domains only
- [ ] Rate limiting enabled with appropriate thresholds
- [ ] Audit logging enabled and shipping to SIEM
- [ ] HSTS header set to 1 year max-age
- [ ] CSP header restrictive (no unsafe-inline in production)
- [ ] Security headers present (X-Frame-Options, etc.)
- [ ] No debug information in error responses
- [ ] Load balancer has DDoS protection enabled
- [ ] Database backups encrypted and tested
- [ ] Dependency scan passed (no critical vulnerabilities)
- [ ] SAST scan passed (no high-risk findings)
- [ ] Penetration test completed (annual or before major release)
- [ ] Compliance audit completed (GDPR, SOC2, etc.)

---

## Summary of Architecture Decisions

| Decision | Rationale |
|----------|-----------|
| **3 Microservices** | Independent scaling, clear boundaries, extreme scale support |
| **HTTP REST APIs** | Simple, observable, easy local testing, widely supported |
| **Separate Databases** | Strong consistency per service, independent scaling |
| **L1+L2 Caching** | Sub-millisecond conversions, reduced service-to-service calls |
| **Idempotency Keys** | Safety for retries, exactly-once semantics |
| **Docker Compose Locally** | Matches production environment, all in one laptop |
| **Scalar + OpenAPI 3.1** | Modern UX, tool-agnostic, easily replaceable |
| **Event-Ready (not async yet)** | Future-proof for RabbitMQ/Azure Service Bus adoption |

---

## Next Steps

1. ✅ **Design approved** (this document)
2. → **Implementation plan** (writing-plans skill)
3. → **Milestone 1:** Create Auth Service (users, roles, JWT tokens)
4. → **Milestone 2:** Create Catalog Service (units, approvals)
5. → **Milestone 3:** Create Conversion Service (high-scale conversions)
6. → **Milestone 4:** Docker Compose orchestration & local testing
7. → **Milestone 5:** Azure deployment & CI/CD pipeline

---

## Appendix: Useful References

### Related Architecture Documents
- **Resilience Architecture:** See [RESILIENCE-ARCHITECTURE.md](../../../RESILIENCE-ARCHITECTURE.md)
  - Inbound rate limiting with `Microsoft.AspNetCore.RateLimiting` (DDoS protection)
  - Outbound HTTP resilience with `Microsoft.Extensions.Http.Resilience` (standard & custom pipelines)
  - General resilience patterns with `Microsoft.Extensions.Resilience`
  - Monitoring, testing, and deployment strategies
- **Contracts & Paging:** See [ARCHITECTURE-CONTRACTS-PAGING-RATELIMIT.md](../ARCHITECTURE-CONTRACTS-PAGING-RATELIMIT.md)

### Standards & Specifications
- **RFC 7807:** Problem Details for HTTP APIs (error standardization)
- **RFC 7230-7236:** HTTP Semantics and Connection (idempotency)
- **OpenAPI 3.1 Spec:** https://spec.openapis.org/oas/v3.1.0
- **CQRS Pattern:** https://martinfowler.com/bliki/CQRS.html
- **Domain-Driven Design:** Evans, E. (2003). Domain-Driven Design.

### .NET Resilience & Cloud References
- **Building resilient cloud services with .NET 8:** https://devblogs.microsoft.com/dotnet/building-resilient-cloud-services-with-dotnet-8/
- **Introduction to resilient app development - .NET:** https://learn.microsoft.com/en-us/dotnet/core/resilience/
- **Polly Documentation:** https://www.pollydocs.org/
- **ASP.NET Core Rate Limiting:** https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit
- **Azure DDoS Protection Standard:** https://learn.microsoft.com/en-us/azure/ddos-protection/ddos-protection-standard-features
- **OpenTelemetry .NET:** https://opentelemetry.io/docs/instrumentation/net/
