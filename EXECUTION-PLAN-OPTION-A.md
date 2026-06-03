# EXECUTION PLAN: Option A - Full Refactoring + Milestone 1 Completion

**Status**: LIVE EXECUTION  
**Total Duration**: 16-17 hours  
**Strategy**: Parallel execution where possible, sequential where dependent

---

## PHASE 1: REFACTORING (2.5 hours, can parallelize to ~1.5 hours)

### Refactoring Tasks (R1-R5)

#### **R1: Create Contracts Projects + Records** (1 hour) - PARALLEL READY
**Deliverables**:
- [ ] New project: `src/UnitConverter.Contracts/` with .csproj
- [ ] Auth records (Commands, Responses) as per REFACTORING-CODE-TEMPLATES.md Template 1
- [ ] Update Auth.Application to reference UnitConverter.Contracts
- [ ] Verify JSON serialization works (System.Text.Json)
- [ ] Tests: 5+ tests for record contracts

#### **R2: Convert DTOs to Records** (1 hour) - DEPENDS ON R1
**Deliverables**:
- [ ] RegisterUserRequest → record
- [ ] LoginRequest → record
- [ ] TokenResponse → record
- [ ] UserResponse → record
- [ ] ErrorResponse → record
- [ ] All existing handlers updated to use records
- [ ] Tests: Verify pattern matching works

#### **R3: Move Middleware to Auth.Api** (1.5 hours) - PARALLEL READY
**Deliverables**:
- [ ] Create `src/UnitConverter.Auth/API/Middleware/` folder
- [ ] Move ExceptionHandlingMiddleware from UnitConverter.Core
- [ ] Move SecurityHeadersMiddleware from UnitConverter.Core
- [ ] Move AuditLoggingMiddleware from UnitConverter.Core
- [ ] Move RequestLoggingMiddleware from UnitConverter.Core
- [ ] Create MiddlewareExtensions.cs
- [ ] Update Auth.Api/Program.cs to register middleware in correct order
- [ ] Remove middleware from UnitConverter.Core
- [ ] Tests: 10+ middleware tests

#### **R4: Decouple UnitConverter.Core** (1 hour) - PARALLEL READY
**Deliverables**:
- [ ] Remove any Auth, Catalog, Conversion references from Core
- [ ] Create IResilienceConfigurer interface (per Template 3)
- [ ] Core.Extensions/ServiceCollectionExtensions.cs: AddCoreResilience()
- [ ] Verify Core only depends on Microsoft.Extensions.*
- [ ] Update Auth.Api/Program.cs to use new interface
- [ ] Tests: 5+ tests for IResilienceConfigurer

#### **R5: LocalDB Configuration** (1 hour) - PARALLEL READY
**Deliverables**:
- [ ] Create `appsettings.Development.json` with LocalDB connection string
- [ ] Update `appsettings.json` for production SQL Server
- [ ] Update Auth.Api/Program.cs for dual configuration
- [ ] Create database initialization in Program.cs (automatic migrations)
- [ ] Update test fixtures to use SQLite in-memory
- [ ] Verify migrations apply automatically on startup
- [ ] Tests: 3+ tests for LocalDB + SQLite configuration

---

## PHASE 2: IMPLEMENTATION (14 hours)

### Implementation Tasks (Tasks 7-13)

#### **Task 7: LoginCommandHandler + Validator** (2 hours)
**Deliverables**:
- [ ] LoginCommand record (already in R1/R2)
- [ ] LoginCommandHandler (implement ICommandHandler<LoginCommand, TokenResponse>)
- [ ] LoginValidator (FluentValidation)
- [ ] Password verification logic
- [ ] JWT token generation via ITokenGenerator
- [ ] Tests: 15+ BDD-organized tests
  - Happy path: valid credentials → returns JWT + refresh token
  - Invalid email
  - Invalid password
  - User not found
  - Account inactive
  - Edge cases (empty strings, SQL injection attempts)

#### **Task 8: RefreshToken + RevokeToken Handlers** (2 hours)
**Deliverables**:
- [ ] RefreshTokenCommand record
- [ ] RevokeTokenCommand record
- [ ] RefreshTokenCommandHandler
- [ ] RevokeTokenCommandHandler
- [ ] RefreshToken validation (expired, revoked, blacklisted)
- [ ] Token blacklist logic
- [ ] Tests: 12+ tests
  - Valid refresh token → new JWT
  - Expired refresh token → 401
  - Blacklisted token → 401
  - Revoked token → 401

#### **Task 9: API Controllers** (2 hours)
**Deliverables**:
- [ ] AuthController: POST /api/v1/register, /api/v1/login, /api/v1/refresh-token, /api/v1/logout
- [ ] InternalController: POST /api/internal/validate-token (inter-service)
- [ ] Rate limiting decorators: @RequireRateLimiting(...)
- [ ] Return proper error responses (ProblemDetails)
- [ ] Response headers (X-Correlation-ID)
- [ ] Tests: 10+ controller tests using WebApplicationFactory

#### **Task 10: Dependency Injection Setup** (1.5 hours)
**Deliverables**:
- [ ] Update Auth.Api/Extensions/ServiceCollectionExtensions.cs
- [ ] Register all domain services (CommandHandlers, Validators, etc.)
- [ ] Register infrastructure (DbContext, repositories, UnitOfWork)
- [ ] Call AddServiceDefaults() for health + OTel
- [ ] Call AddCoreResilience() for rate limiting
- [ ] Verify no circular dependencies
- [ ] Tests: 5+ DI resolution tests

#### **Task 11: Security Middleware Implementation** (2 hours)
**Deliverables**:
- [ ] Implement ExceptionHandlingMiddleware (if not done in R3)
- [ ] Implement SecurityHeadersMiddleware with HSTS, CSP, X-Frame-Options
- [ ] Implement AuditLoggingMiddleware with audit logging
- [ ] Implement RequestLoggingMiddleware with correlation IDs
- [ ] Middleware pipeline ordering in Program.cs
- [ ] Tests: 8+ middleware tests (from R3)

#### **Task 12: Integration Tests** (3 hours)
**Deliverables**:
- [ ] Full auth flow: register → login → get token → refresh → logout
- [ ] Rate limiting: verify 429 responses
- [ ] Health checks: verify /health and /alive endpoints
- [ ] Internal token validation: verify /api/internal/validate-token
- [ ] Error handling: verify ProblemDetails responses
- [ ] Middleware: verify headers, correlation IDs, audit logs
- [ ] Tests: 20+ integration tests using WebApplicationFactory

#### **Task 13: Docker & AppHost Setup** (1.5 hours)
**Deliverables**:
- [ ] Create Dockerfile (multi-stage build)
- [ ] Create .dockerignore
- [ ] Create src/UnitConverter.AppHost/Program.cs
- [ ] docker-compose.yml (local development)
- [ ] Verify health endpoints work
- [ ] Verify Aspire dashboard can access services
- [ ] Test local startup: `dotnet run --project src/UnitConverter.AppHost`
- [ ] Tests: 2+ integration tests for Docker

---

## EXECUTION STRATEGY

### Parallelization Groups

**Group 1** (Start immediately, run in parallel - 1.5 hours):
- R1: Create Contracts projects (1 hour)
- R3: Move middleware to Auth.Api (1.5 hours)
- R4: Decouple UnitConverter.Core (1 hour)
- R5: LocalDB configuration (1 hour)

**Group 2** (After R1 completes, ~1 hour):
- R2: Convert DTOs to records (1 hour)

**Group 3** (After R1, R2, R3, R4, R5 - Sequential, ~14 hours):
- Task 7: LoginCommandHandler (2 hours)
- Task 8: RefreshToken handlers (2 hours)
- Task 9: API Controllers (2 hours)
- Task 10: DI Setup (1.5 hours)
- Task 11: Security Middleware complete (2 hours)
- Task 12: Integration Tests (3 hours)
- Task 13: Docker & AppHost (1.5 hours)

---

## COMMIT STRATEGY

**After R1-R5 Complete** (2.5 hours):
```
git commit -m "refactor: contracts-based architecture with records, localdb, and middleware organization"
```

**After R1-R5 + Task 7** (4.5 hours):
```
git commit -m "feat: implement LoginCommandHandler with BDD tests"
```

**After Task 8** (6.5 hours):
```
git commit -m "feat: implement RefreshToken and RevokeToken handlers"
```

**After Task 9** (8.5 hours):
```
git commit -m "feat: implement Auth API controllers with rate limiting"
```

**After Task 10** (10 hours):
```
git commit -m "feat: complete dependency injection setup and service registration"
```

**After Task 11** (12 hours):
```
git commit -m "feat: finalize security middleware with exception mapping and audit logging"
```

**After Task 12** (15 hours):
```
git commit -m "test: add comprehensive integration test suite for full auth flow"
```

**After Task 13** (16.5 hours):
```
git commit -m "ops: add Dockerfile, docker-compose, and Aspire AppHost orchestration"
```

---

## SUCCESS CRITERIA

### After Refactoring (2.5 hours):
- ✅ UnitConverter.Contracts project created with records
- ✅ All middleware moved to Auth.Api
- ✅ UnitConverter.Core has no business logic references
- ✅ LocalDB works locally without SSMS
- ✅ 0 compiler errors, 0 linter warnings

### After Milestone 1 Complete (16-17 hours):
- ✅ 200+ tests passing (domain + auth + resilience + database + middleware + integration)
- ✅ Full auth flow working (register → login → token → refresh → logout)
- ✅ Rate limiting enforced (3 policies)
- ✅ Security middleware active (exception handling, headers, audit logs, correlation IDs)
- ✅ Health checks available (/health, /alive)
- ✅ Docker deployment ready
- ✅ Aspire AppHost orchestrates all services
- ✅ Production-ready code (9.2/10 score)

---

## ROLLOUT PLAN

**Immediate** (Next 30 minutes):
- Launch R1, R3, R4, R5 subagents in parallel
- You proceed to R2 as soon as R1 completes

**Next 1.5 hours**:
- R1-R5 refactoring completes
- Code is committed
- First commit milestone reached

**Next 14 hours** (Sequential):
- Launch Tasks 7-13 subagents
- Each task completes and commits independently
- Progress visible after each 1-3 hour chunk

**Final State** (16-17 hours):
- Milestone 1 Auth Service COMPLETE
- Production-ready architecture
- 200+ tests passing
- Docker deployment ready
- Aspire orchestration functional

---

## READY TO LAUNCH? ✅

This plan is aggressive but achievable with:
- Parallel refactoring (save ~1 hour)
- Focused implementation (one task at a time)
- Automatic migrations (no manual DB setup)
- Comprehensive testing (20+ tests per handler)

**Proceed?** YES ✅
