# Complete Implementation Execution Plan

**Date**: June 3, 2026  
**Objective**: Complete full Auth Service (Milestone 1) + Foundation for Catalog & Conversion  
**Scope**: Execute Tasks 5-13 of Milestone 1 + quick setup of Milestones 2-3 scaffolding

---

## Current Status: Milestone 1 Tasks Completed

✅ **Task 1**: Core Domain Models (User, Role, Email, UserId, Password, Exceptions)  
✅ **Task 2**: Common Layer Interfaces (IRepository, ITokenGenerator, IPasswordHasher, etc.)  
✅ **Task 3**: JWT Token Service & Password Hashing  
✅ **Task 4**: Register User Command Handler & Validation (260 tests passing)

---

## Remaining Tasks (5-13): Implementation Roadmap

### Phase 1: Resilience & Observability (Task 5)
**Time**: 2-3 hours  
**Parallel**: Can run while infrastructure is being built

- [ ] Create `UnitConverter.ServiceDefaults` project (shared health + OTel)
- [ ] Create `RateLimitingExtensions.cs` (3 partitioned policies)
- [ ] Create `HttpResilienceExtensions.cs` (standard resilience handler)
- [ ] Update `appsettings.json` with rate limiting config

### Phase 2: Database & Infrastructure (Task 6)
**Time**: 3-4 hours  
**Blocking**: Required before handlers

- [ ] Create `AuthDbContext` (EF Core, SQL Server / SQLite)
- [ ] Create `UserRepository` + `RoleRepository` (implement IUserRepository, IRoleRepository)
- [ ] Create migrations
- [ ] Create `UnitOfWork` pattern implementation
- [ ] Add database health checks

### Phase 3: Business Logic Handlers (Tasks 7-8)
**Time**: 2-3 hours  
**Blocking**: After Task 6

- [ ] Task 7: LoginCommandHandler + LoginValidator
- [ ] Task 8: RefreshTokenCommandHandler + RevokeTokenCommandHandler  
- [ ] Add query handlers (ValidateToken, GetUserById, GetUserRoles)
- [ ] Tests: 20+ tests per handler (BDD-organized)

### Phase 4: API Controllers (Task 9)
**Time**: 2 hours

- [ ] AuthController: `/register`, `/login`, `/refresh-token`, `/logout`
- [ ] InternalController: `/validate-token` (for inter-service calls)
- [ ] Apply rate limiting decorators per endpoint
- [ ] Return proper error responses (ProblemDetails)

### Phase 5: Dependency Injection (Task 10)
**Time**: 1.5 hours

- [ ] Create `ServiceCollectionExtensions` (register all layers)
- [ ] Register ServiceDefaults (health checks, resilience, OTel)
- [ ] Update `Program.cs` to call extensions
- [ ] Verify DI container wiring

### Phase 6: Security Middleware (Task 11)
**Time**: 2 hours

- [ ] ExceptionHandlingMiddleware (429, 503, 504, 400, 401, 403, 500)
- [ ] AuditLoggingMiddleware (log user actions, failures)
- [ ] SecurityHeadersMiddleware (HSTS, CSP, etc.)
- [ ] RequestLoggingMiddleware (correlation IDs, trace context)

### Phase 7: Integration Tests (Task 12)
**Time**: 2-3 hours

- [ ] AuthControllerTests (register, login, refresh, logout)
- [ ] RateLimitingTests (verify 429 responses)
- [ ] HealthCheckTests (verify `/health` + `/alive` endpoints)
- [ ] TokenEndpointTests (inter-service validation)
- [ ] Target: 20+ new integration tests

### Phase 8: Docker & Local Development (Task 13)
**Time**: 1.5 hours

- [ ] Create `UnitConverter.AppHost` (Aspire orchestration)
- [ ] Create `docker-compose.yml` (fallback)
- [ ] Verify local health endpoints
- [ ] Verify Aspire dashboard (http://localhost:19888)

---

## Execution Strategy: Parallel Subagents

**Recommended**: Use subagent-driven-development skill to parallelize independent tasks.

### Parallelization Groups

**Group A (Can run immediately)**:
- Task 5a: ServiceDefaults project creation
- Task 6a: Database design + EF Core setup
- Task 11: Security Middleware

**Group B (Waits for Task 6)**:
- Task 7: LoginCommandHandler + Validator
- Task 8: RefreshToken handlers

**Group C (Waits for Tasks 7-8)**:
- Task 9: API Controllers

**Group D (Waits for Task 9)**:
- Task 10: Dependency Injection

**Group E (Parallel to others)**:
- Task 12: Integration Tests (can scaffold while tasks 7-9 are in progress)
- Task 13: Docker setup (can start early, refine as tasks complete)

---

## Success Criteria per Task

### Task 5: Resilience ✅
- [ ] Rate limiting middleware configured (3 policies working)
- [ ] HTTP clients have resilience handlers
- [ ] Configuration can be updated dynamically
- [ ] Tests: 5+ unit tests for rate limiting logic

### Task 6: Database ✅
- [ ] DbContext compiles and migrations work
- [ ] Repositories implement interfaces correctly
- [ ] UnitOfWork manages transactions
- [ ] Health check tests: 3+ tests (can connect, operations work)
- [ ] Tests: 10+ data access tests

### Task 7: Login ✅
- [ ] LoginCommandHandler implements ICommandHandler
- [ ] LoginValidator validates email + password
- [ ] Returns JWT token + refresh token on success
- [ ] Returns error if user not found or credentials invalid
- [ ] Tests: 15+ tests (BDD: happy path, validation, edge cases)

### Task 8: Refresh Token ✅
- [ ] RefreshTokenCommandHandler validates refresh token
- [ ] Returns new JWT token if valid
- [ ] Returns error if token expired or blacklisted
- [ ] RevokeTokenCommandHandler adds token to blacklist
- [ ] Tests: 12+ tests (valid, expired, revoked tokens)

### Task 9: Controllers ✅
- [ ] POST /api/auth/register → 201 Created
- [ ] POST /api/auth/login → 200 OK + tokens
- [ ] POST /api/auth/refresh-token → 200 OK + new JWT
- [ ] POST /api/auth/logout → 204 No Content
- [ ] POST /api/internal/validate-token → 200 OK + user info or 401 Unauthorized
- [ ] Rate limiting decorators applied
- [ ] Tests: 10+ controller tests

### Task 10: DI ✅
- [ ] ServiceCollectionExtensions registers all dependencies
- [ ] Program.cs calls AddServiceDefaults() + AddAuthServices()
- [ ] All interfaces resolved correctly
- [ ] No circular dependencies
- [ ] Tests: 5+ DI resolution tests

### Task 11: Middleware ✅
- [ ] ExceptionHandlingMiddleware catches all exceptions
- [ ] Rate limit rejections return 429 with retry-after header
- [ ] Audit logging captures user actions + failures
- [ ] Security headers present in all responses
- [ ] Correlation IDs flow through traces
- [ ] Tests: 8+ middleware tests

### Task 12: Integration Tests ✅
- [ ] Full auth flow works (register → login → token → logout)
- [ ] Rate limiting blocks excess requests (429)
- [ ] Health endpoints return correct status
- [ ] Internal token validation works
- [ ] Tests: 20+ integration tests using WebApplicationFactory

### Task 13: Docker ✅
- [ ] AppHost runs all services in correct dependency order
- [ ] docker-compose provides fallback
- [ ] Health checks verify service readiness
- [ ] Aspire dashboard shows traces + metrics
- [ ] Local testing works without cloud resources

---

## Test Coverage Goals

| Layer | Existing | New Tasks | Total |
|-------|----------|-----------|-------|
| Domain | 39 tests | +10 (Task 8) | 49 tests |
| Application | 137 tests | +50 (Tasks 7-8) | 187 tests |
| API | 0 tests | +20 (Task 9) | 20 tests |
| Integration | 0 tests | +20 (Task 12) | 20 tests |
| Resilience | 0 tests | +5 (Task 5) | 5 tests |
| Middleware | 0 tests | +8 (Task 11) | 8 tests |
| **Total** | **176 tests** | **+113 tests** | **289 tests** |

---

## Timeline Estimate

| Task | Hours | Notes |
|------|-------|-------|
| Task 5 (Resilience) | 2.5 | Can run in parallel |
| Task 6 (Database) | 3.5 | Blocking for Tasks 7-8 |
| Task 7 (Login) | 2 | After Task 6 |
| Task 8 (Refresh) | 2 | After Task 6 |
| Task 9 (Controllers) | 2 | After Tasks 7-8 |
| Task 10 (DI) | 1.5 | After Task 9 |
| Task 11 (Middleware) | 2 | Can run in parallel |
| Task 12 (Tests) | 3 | After Controllers done |
| Task 13 (Docker) | 1.5 | Can start early, refine later |
| **Total** | **20 hours** | 3-4 days focused work |

**With Parallel Execution**: 12-15 hours (2-3 days)

---

## Milestone 2-3 Scaffold (After Milestone 1)

### Quick Setup (1-2 hours each)

**Milestone 2: Catalog Service**
- [ ] Copy UnitConverter.ServiceDefaults references
- [ ] Create UnitConverter.Catalog folder structure
- [ ] Define UnitCategory, Unit domain models
- [ ] Create ApprovalWorkflow (Pending → Approved/Rejected)
- [ ] Create CatalogDbContext
- [ ] Define API endpoints (CRUD + approval)

**Milestone 3: Conversion Service**
- [ ] Create UnitConverter.Conversion folder structure
- [ ] Define ConversionRequest/ConversionResult
- [ ] Call Catalog Service to fetch approved units
- [ ] Cache results (HybridCache L1 + Redis L2)
- [ ] Define conversion algorithm (linear factor, affine)
- [ ] Define API endpoint (POST /convert)

---

## Next Steps: Immediate Actions

1. **Review this plan** (10 min)
2. **Confirm parallelization strategy** (5 min)
3. **Launch Task 5, 6, 11 subagents in parallel** (start immediately)
4. **Monitor progress** (check hourly)
5. **Sequence Tasks 7-9** as Task 6 completes
6. **Start Task 12** scaffolding while Tasks 7-8 in progress
7. **Finalize Task 10** once all dependencies ready
8. **Run full integration test suite** (verify 289+ tests passing)
9. **Local Docker validation** (AppHost + docker-compose)
10. **Complete Milestone 1** commit

---

## Key Files to Create/Modify

### Priority: HIGH (Blocking path)
- `src/UnitConverter.Auth/Infrastructure/Data/AuthDbContext.cs`
- `src/UnitConverter.Auth/Infrastructure/Repositories/UserRepository.cs`
- `src/UnitConverter.Auth/Infrastructure/Repositories/RoleRepository.cs`
- `src/UnitConverter.Auth/Application/Commands/LoginCommand.cs`
- `src/UnitConverter.Auth/Application/Handlers/LoginCommandHandler.cs`
- `src/UnitConverter.Auth/Application/Commands/RefreshTokenCommand.cs`

### Priority: MEDIUM (Parallel)
- `src/UnitConverter.ServiceDefaults/Extensions.cs`
- `src/UnitConverter.Auth/API/Middleware/SecurityHeadersMiddleware.cs`
- `src/UnitConverter.Auth/API/Middleware/ExceptionHandlingMiddleware.cs`

### Priority: LOW (After)
- `src/UnitConverter.Auth/API/Controllers/AuthController.cs`
- `src/UnitConverter.AppHost/Program.cs`
- `docker-compose.yml`

---

## Ready to Execute?

**This plan is actionable and ready for immediate implementation. Confirm to proceed with Task 5 (Resilience).**

Would you like me to:
1. **Start Task 5** (Resilience setup) now?
2. **Start Tasks 5, 6, 11 in parallel** using subagents?
3. **Modify plan** based on your preferences?
4. **Show code templates** for immediate implementation?
