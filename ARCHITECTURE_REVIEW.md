# UnitConverter System Design & Best Practices Review

**Review Date:** 2026  
**Project:** UnitConverter API & Management System  
**.NET Version:** .NET 10  
**Architecture:** Clean Architecture with Razor Pages Web UI

---

## Executive Summary

Your UnitConverter project is **well-architected and demonstrates professional software engineering practices**. The README is comprehensive and the architectural vision is clearly articulated. This review identifies:

✅ **Strengths** — solid foundations, SOLID principles, test-driven design, security-first mindset  
⚠️ **Areas for Improvement** — gaps between documented architecture and actual implementation, missing concrete examples, deployment clarity  
🔧 **Recommended Enhancements** — documentation completeness, CI/CD guidance, monitoring setup steps

---

## 1. Architecture Assessment

### 1.1 ✅ Clean Architecture Implementation

**Strengths:**
- Clear separation of concerns with Domain → Application → Infrastructure → API layers
- Domain layer explicitly free of framework dependencies (ASP.NET, EF Core)
- Repository pattern enables multi-database support (SQLite/SQL Server/PostgreSQL)
- ADRs (Architecture Decision Records) are comprehensive and well-documented

**Current State vs. Documented:**
```
✅ Documented architecture is ambitious and sound
⚠️ README claims implementation is underway, but actual project structure shows:
   - UnitConverter.Auth (not mentioned in docs)
   - Multiple test projects already in place
   - Infrastructure layer exists alongside Domain/Application
```

**Recommendations:**
1. **Update HLD.md §8 to reflect actual structure** including `UnitConverter.Auth` project
2. **Add a "Project Status" section to README** mapping which features are scaffolded vs. in-progress vs. complete
3. **Create a quick-start diagram** showing how the projects fit together

---

### 1.2 ✅ Test-Driven Development (TDD) & BDD

**Strengths:**
- MSTest + Moq for unit/integration tests
- Reqnroll for BDD scenarios
- BenchmarkDotNet for performance regression testing
- Three-tier test pyramid (unit → integration → BDD + load tests)

**Current Issues:**
```
✅ Test infrastructure is well-planned
⚠️ README doesn't clearly show which tests are runnable today vs. placeholders
⚠️ No link to test execution examples or CI/CD pipeline
```

**Recommendations:**
1. **Add "Test Coverage" section to README:**
   ```markdown
   ## Test Coverage

   Run all tests:
   ```bash
   dotnet test
   ```

   By category:
   - **Unit Tests:** `dotnet test --filter "Category=Unit"`
   - **Integration Tests:** `dotnet test --filter "Category=Integration"`
   - **BDD Tests:** `dotnet test tests/UnitConverter.Load.Tests`
   - **Performance:** `dotnet run --project perf/UnitConverter.Benchmarks`
   ```

2. **Create a test strategy document** (`docs/TESTING.md`) with:
   - Example: how to add a new unit test
   - Example: how to write a BDD scenario
   - Target coverage %
   - CI/CD gate criteria

3. **Verify test projects are linked in the solution** (currently shows structure, not clarity)

---

### 1.3 ⚠️ Security Posture

**Documented (✅ Good):**
- OWASP Top 10 baseline mentioned
- ADR-0011 covers SAST/SCA/DAST
- ADR-0003 security & error-handling baseline
- Role-based authorization (Public, Employee, Partner, Admin)
- Partner API key authentication (ADR-0013)

**Implementation Gaps (⚠️ To Address):**
1. **No concrete security guidelines in code** — missing SECURITY.md
2. **No reference to .NET security analyzers** — SecurityCodeScan, NetAnalyzers status unclear
3. **No mention of input validation library** — Fluent Validation? DataAnnotations? Custom?
4. **No encryption guidance** — at-rest and in-transit strategies not documented

**Recommendations:**
1. **Create `docs/SECURITY.md`** with sections:
   ```markdown
   # Security Guidelines

   ## Input Validation
   - Use Fluent Validation for request DTOs (chain of validators)
   - Fail-fast principle: validate at the API layer
   - Example: [Link to validation code]

   ## Authorization
   - Policy-based: `[Authorize(Policy = "CanApproveUnits")]`
   - No claim parsing in controllers; use named policies (see AuthorizationService)
   - Role hierarchy: Admin > Partner ≥ Employee > Public

   ## Sensitive Data
   - API keys: hashed in database, never logged
   - Passwords: bcrypt via ASP.NET Identity
   - PII: tagged with [SensitiveData] for masking in logs/traces

   ## SAST/SCA/DAST
   - Run locally: `dotnet tool run roslynator analyze`
   - CI: Dependabot + CodeQL on each PR
   - Annual pen testing (budget & scope)

   ## HTTPS & TLS
   - Enforced in production via middleware
   - Dev: HTTPS opt-out only if behind proxy
   - Certificate pinning: not required for v1, revisit if mobile client added
   ```

2. **Enable and document security analyzers in projects:**
   ```xml
   <!-- In .csproj -->
   <PropertyGroup>
	   <EnableNETAnalyzers>true</EnableNETAnalyzers>
	   <AnalysisLevel>latest</AnalysisLevel>
   </PropertyGroup>
   ```

3. **Add CI/CD gates** for SAST/SCA in GitHub Actions

---

### 1.4 ⚠️ Observability & Monitoring (Documented but Not Tested)

**Documented (✅):**
- OpenTelemetry + Aspire dashboard for local development
- C# interceptors for cross-cutting logging
- HybridCache for performance
- Structured logging with correlation IDs

**Implementation Status (⚠️):**
- No concrete example of running the Aspire dashboard
- No guidance on setting up OpenTelemetry exporter for production (Jaeger/Seq/DataDog/Azure Monitor)
- No alerting rules documented

**Recommendations:**
1. **Create `docs/OBSERVABILITY.md`:**
   ```markdown
   # Observability Setup

   ## Local Development (Aspire Dashboard)

   1. Run the AppHost:
	  ```bash
	  dotnet run --project src/UnitConverter.ServiceDefaults
	  ```
   2. Open http://localhost:18888 (Aspire dashboard)
   3. Trigger a conversion: `POST http://localhost:5000/api/conversions`
   4. Inspect traces, metrics, logs in the dashboard

   ## Production Setup (Example: Azure Monitor)

   1. Create Application Insights resource
   2. Set environment: `OTEL_EXPORTER_OTLP_ENDPOINT=https://...:443`
   3. Deploy: traces → Application Insights → Alerts

   ## Key Metrics to Monitor
   - `conversions.count` (RPS)
   - `conversions.duration` (latency p95/p99)
   - `conversions.errors.count` (error rate)
   - HTTP status code distribution
   ```

2. **Add health check endpoints:**
   ```csharp
   app.MapHealthChecks("/health/live");      // liveness
   app.MapHealthChecks("/health/ready");     // readiness
   ```
   Document these in HLD.md §11 (Deployment).

3. **Document alerting thresholds** (response time, error rate, availability)

---

### 1.5 ⚠️ Deployment & Cloud Readiness

**Documented (✅):**
- Stateless architecture for horizontal scaling
- Multi-database support
- Aspire manifests for ACA/AKS/K8s
- 12-factor configuration principles

**Implementation Gaps (⚠️):**
1. No Dockerfile in repo
2. No example Aspire manifest (AppHost) in docs
3. No database migration strategy for EF Core
4. No load balancer configuration shown

**Recommendations:**
1. **Create `Dockerfile` in root:**
   ```dockerfile
   FROM mcr.microsoft.com/dotnet/sdk:10 AS build
   WORKDIR /app
   COPY . .
   RUN dotnet publish -c Release -o /out

   FROM mcr.microsoft.com/dotnet/aspnet:10
   WORKDIR /app
   COPY --from=build /out .
   EXPOSE 80 443
   ENTRYPOINT ["dotnet", "UnitConverter.Api.dll"]
   ```

2. **Create `.dockerignore`:**
   ```
   .git
   .github
   bin
   obj
   .vs
   *.db
   *.log
   ```

3. **Document deployment strategies** (`docs/DEPLOYMENT.md`):
   ```markdown
   ## Local (Development)
   ```bash
   dotnet run --project src/UnitConverter.Api
   ```

   ## Docker
   ```bash
   docker build -t unitconverter:latest .
   docker run -p 5000:80 unitconverter:latest
   ```

   ## Azure Container Apps (ACA)
   ```bash
   azd up
   ```

   ## Kubernetes (AKS)
   ```bash
   dotnet aspire init
   kubectl apply -f manifests/
   ```
   ```

4. **Add database migration automation:**
   ```csharp
   // Program.cs
   using var scope = app.Services.CreateScope();
   var db = scope.ServiceProvider.GetRequiredService<UnitConverterDbContext>();
   await db.Database.MigrateAsync();
   ```

---

## 2. Best Practices Assessment

### 2.1 ✅ Code Organization

**Strengths:**
- Clear project structure (src / tests / perf / docs)
- Domain-driven design with value objects and aggregates
- Immutable types (`record` for DTOs, entities)
- No circular dependencies (dependencies flow inward)

**Recommendations:**
1. **Add EditorConfig examples** to docs (formatting, naming conventions)
2. **Document which files belong where** with a CONTRIBUTING.md guide

---

### 2.2 ✅ SOLID Principles

**Single Responsibility Principle (SRP):**
- ✅ ConversionRule implementations are focused
- ✅ Use cases separate from controllers
- ⚠️ Infrastructure might be loading units AND managing cache (verify)

**Open/Closed Principle (OCP):**
- ✅ New units don't require endpoint changes
- ✅ Strategy pattern (IConversionRule) allows new rules

**Liskov Substitution Principle (LSP):**
- ✅ IConversionRule implementations are substitutable
- ⚠️ Verify all IRepository implementations behave identically

**Interface Segregation Principle (ISP):**
- ⚠️ Large IRepository interface might violate ISP (check repo sizes)

**Dependency Inversion Principle (DIP):**
- ✅ Clear use of dependency injection
- ✅ Infrastructure implements Application ports

**Recommendations:**
1. **Add SOLID enforcement checklist** to ADR-0012 with code examples
2. **Add pre-commit hooks** to prevent SRP violations (requires code review criteria)

---

### 2.3 ⚠️ Error Handling

**Documented (✅):**
- RFC 7807 ProblemDetails for errors
- Domain exceptions for broken invariants

**Implementation Issues (⚠️):**
1. No centralized exception mapping shown
2. No guidance on custom exception types
3. No distinction between user errors (400) vs. system errors (500)

**Recommendations:**
1. **Create exception hierarchy** in Domain:
   ```csharp
   public abstract class DomainException : Exception;
   public sealed class IncompatibleUnitsException : DomainException;
   public sealed class UnknownUnitException : DomainException;
   ```

2. **Add exception mapping middleware** in API startup:
   ```csharp
   app.UseExceptionHandler(errorApp =>
   {
	   errorApp.Run(async context =>
	   {
		   var exception = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
		   var problemDetails = exception switch
		   {
			   DomainException de => new ProblemDetails
			   {
				   Status = StatusCodes.Status400BadRequest,
				   Title = "Domain Error",
				   Detail = de.Message
			   },
			   _ => new ProblemDetails
			   {
				   Status = StatusCodes.Status500InternalServerError,
				   Title = "Internal Error"
			   }
		   };
		   // Write problemDetails to response
	   });
   });
   ```

3. **Document in LLD.md §7** (API Contract section)

---

### 2.4 ✅ Caching Strategy

**Documented (✅):**
- HybridCache for L1 (in-memory) and L2 (Redis) caching
- Catalog reads cached (immutable data)
- Conversion arithmetic not cached (cheap operation)

**Verification Needed:**
- ⚠️ Verify cache invalidation strategy when units are approved/rejected

**Recommendations:**
1. **Document cache invalidation** in Infrastructure:
   ```csharp
   // When a unit is approved
   _cache.Remove("units:approved");
   _cache.Remove("categories");
   ```

2. **Add cache hit/miss metrics** to observability

---

### 2.5 ⚠️ API Versioning

**Current State:**
- No API versioning mentioned in README
- Route is `/api/conversions` (v1 implied but not explicit)

**Recommendations:**
1. **Add API versioning strategy** (`docs/API_VERSIONING.md`):
   ```markdown
   # API Versioning

   Routes include version in path: `/api/v1/conversions`

   - Changes to response structure → new version
   - Addition of optional query params → same version
   - Breaking removals → deprecated, then removed 2 versions later
   ```

2. **Update HLD.md §6** to show versioned routes

---

### 2.6 ⚠️ Rate Limiting & DDoS Protection

**Current State:**
- Not mentioned in README
- Infrastructure noted as "ready" (ADR-0005)

**Recommendations:**
1. **Add rate limiting middleware** (e.g., AspNetCoreRateLimit):
   ```csharp
   services.AddMemoryCache();
   services.AddInMemoryRateLimiting();
   app.UseIpRateLimiting();
   ```

2. **Document in ADR or SECURITY.md**

---

### 2.7 ⚠️ Logging & Correlation IDs

**Documented (✅):**
- C# interceptors for logging
- Structured logging with correlation IDs

**Implementation Check:**
- ⚠️ Need to verify correlation ID header parsing (W3C traceparent)

**Recommendations:**
1. **Add middleware for correlation ID extraction:**
   ```csharp
   app.Use(async (context, next) =>
   {
	   var traceId = context.Request.Headers.GetValueOrDefault("traceparent") 
					 ?? Activity.Current?.Id 
					 ?? context.TraceIdentifier;
	   using var activity = new Activity("RequestProcessing").Start();
	   activity.SetTag("http.request.body.original_content_length", 
					   context.Request.ContentLength);
	   await next(context);
   });
   ```

2. **Document in Observability.md**

---

### 2.8 ⚠️ Dependency Management

**Current State:**
- README mentions DEPENDENCIES.md
- Dependabot strategy in ADR-0015

**Recommendations:**
1. **Verify DEPENDENCIES.md exists and is up-to-date**
2. **Enable GitHub Dependabot** (if not already):
   ```yaml
   # .github/dependabot.yml
   version: 2
   updates:
	 - package-ecosystem: "nuget"
	   directory: "/"
	   schedule:
		 interval: "weekly"
	   open-pull-requests-limit: 5
   ```

3. **Create NuGet dependency audit checklist:**
   - Security vulnerabilities scanned (Dependabot)
   - License compliance (no GPL in a commercial product if applicable)
   - Maintenance status (prefer actively maintained packages)

---

## 3. Documentation Gaps & Recommendations

### 3.1 Missing Documents

| Document | Purpose | Priority |
|----------|---------|----------|
| `CONTRIBUTING.md` | How to add new features, run tests, code style | **High** |
| `docs/SECURITY.md` | Security practices, guidelines, incident response | **High** |
| `docs/TESTING.md` | How to write unit/integration/BDD tests | **High** |
| `docs/DEPLOYMENT.md` | Docker, ACA, AKS, CI/CD pipeline setup | **Medium** |
| `docs/OBSERVABILITY.md` | How to set up and use Aspire dashboard, production monitoring | **Medium** |
| `docs/API_VERSIONING.md` | Breaking vs. non-breaking changes, deprecation policy | **Medium** |
| `docs/PERFORMANCE.md` | Benchmarking, load testing, optimization tips | **Low** |
| `.github/ISSUE_TEMPLATE/bug_report.md` | Standardized bug reports | **Low** |
| `.github/PULL_REQUEST_TEMPLATE.md` | PR checklist (tests, docs, CHANGELOG) | **Low** |

### 3.2 README Improvements

**Current README is comprehensive but could benefit from:**

1. **Add a "Quick Links" section:**
   ```markdown
   ## Quick Start

   - **I want to...** [run the API](#getting-started) | [write a test](docs/TESTING.md) | [add a new unit](#unit-management-workflow) | [deploy to Azure](#deployment)
   - **Learn more:** [Architecture](docs/HLD.md) | [Data model](docs/LLD.md) | [Security](docs/SECURITY.md) | [API Docs](#)
   - **Roadmap:** [Milestones](docs/PLAN.md)
   - **Need help?** [Open an issue](#) | [See FAQ](#)
   ```

2. **Add "Current Status" section:**
   ```markdown
   ## Project Status

   | Component | Status | Notes |
   |-----------|--------|-------|
   | Domain model | ✅ Complete | Length, Temperature, Weight categories |
   | REST API | 🔄 In Progress | Endpoints defined, conversion logic TBD |
   | Web UI | 🔄 In Progress | Razor Pages scaffolded, forms TBD |
   | Unit approval workflow | ⏳ Planned | State machine, persistence layer |
   | Security (SAST/SCA/DAST) | ⏳ Planned | Baseline defined in ADR-0011 |
   | Observability (Aspire) | ⏳ Planned | Structures defined, integration TBD |
   ```

3. **Add "Troubleshooting" section:**
   ```markdown
   ## Troubleshooting

   **Q: Build fails with "missing project"**
   A: Run `dotnet restore`

   **Q: Tests fail with "database not found"**
   A: Tests use SQLite in-memory; ensure `Microsoft.EntityFrameworkCore.Sqlite` is installed

   **Q: Aspire dashboard won't start**
   A: Check port 18888 is free; run `netstat -ano | findstr 18888`
   ```

---

## 4. Implementation Checklist

### Phase 1: Documentation (Week 1)
- [ ] Create CONTRIBUTING.md
- [ ] Create docs/SECURITY.md
- [ ] Create docs/TESTING.md
- [ ] Update README with "Quick Links" and "Current Status"
- [ ] Update HLD.md to reflect actual project structure (UnitConverter.Auth, etc.)

### Phase 2: Security & Quality (Week 2-3)
- [ ] Enable SAST/SCA in CI/CD pipeline
- [ ] Add exception handling middleware
- [ ] Add rate limiting middleware
- [ ] Add correlation ID middleware
- [ ] Create DEPENDENCIES.md audit checklist

### Phase 3: Observability & Deployment (Week 4-5)
- [ ] Create docs/OBSERVABILITY.md with Aspire setup examples
- [ ] Create docs/DEPLOYMENT.md (Docker, ACA, AKS)
- [ ] Create Dockerfile and .dockerignore
- [ ] Set up health check endpoints
- [ ] Document alerting thresholds

### Phase 4: Testing & Validation (Week 6)
- [ ] Verify all test projects run successfully
- [ ] Document test execution examples in docs/TESTING.md
- [ ] Add test coverage targets (e.g., 80% for Domain)
- [ ] Set up code coverage reports in CI/CD

### Phase 5: API & Versioning (Week 7)
- [ ] Create docs/API_VERSIONING.md
- [ ] Update routes to use `/api/v1/...`
- [ ] Document breaking vs. non-breaking changes

---

## 5. Best Practices Maturity Matrix

| Area | Current | Target | Gap |
|------|---------|--------|-----|
| **Architecture** | ✅ Well-designed | ✅ Production-ready | Implement GH Projects board |
| **Testing** | ✅ Infrastructure in place | ✅ >80% coverage | Need test data & examples |
| **Security** | 🔄 Baseline documented | ✅ SAST/SCA/DAST automated | Enable CI/CD gates |
| **Observability** | 🔄 Architecture defined | ✅ Live dashboards, alerts | Set up Aspire, metrics |
| **Deployment** | 🔄 Principles defined | ✅ One-click deploy (IaC) | Create Dockerfile, ACA manifest |
| **CI/CD** | ⏳ Not documented | ✅ GH Actions, multi-env | Add workflows |
| **Documentation** | 🔄 ADRs excellent | ✅ Step-by-step guides | Add CONTRIBUTING, SECURITY, TESTING |

---

## 6. Quick Wins (Low Effort, High Impact)

1. **Add GitHub issue templates** (15 min) — improves issue quality
2. **Create PR template** (10 min) — ensures test/doc coverage reminder
3. **Add .editorconfig** (20 min) — enforces code style without debate
4. **Create CONTRIBUTING.md** (30 min) — unblocks new contributors
5. **Enable branch protection rules** (10 min) — enforce review + passing tests
6. **Add CODEOWNERS** file (5 min) — auto-assign reviewers

---

## 7. Key Strengths to Build Upon

1. **Clear architectural vision** — ADRs are exemplary
2. **Security-first mindset** — OWASP baseline from day one
3. **Test infrastructure** — TDD, BDD, load testing planned
4. **Cloud-ready design** — stateless, multi-DB, Aspire-ready
5. **Team enablement** — excellent documentation for developers

---

## 8. Final Recommendations

### Priority 1 (Critical): Documentation
Your architecture is solid, but the **gap between documented design and implementation** is confusing. Update README and HLD to reflect real status.

### Priority 2 (Important): Security & Quality Gates
Implement SAST/SCA/DAST in CI/CD; add exception mapping, rate limiting, correlation IDs.

### Priority 3 (Nice-to-Have): Observability Runbooks
Document how to set up Aspire dashboard, production monitoring, alerting — with examples.

### Priority 4 (Enhancement): Deployment Automation
Dockerfile + Aspire manifests + IaC for ACA/AKS.

---

## Conclusion

**Overall Assessment: 8/10 — Excellent architecture, great documentation foundation, needs implementation clarity.**

Your project demonstrates **professional software engineering practices** and is well-positioned for growth. The next phase should focus on:

1. ✅ **Bridge the docs-to-code gap** (status, implementation examples)
2. ✅ **Automate security/quality checks** (CI/CD gates)
3. ✅ **Enable observability** (Aspire, metrics, alerts)
4. ✅ **Simplify deployment** (Docker, IaC, runbooks)

**This is a learning project done *right*** — one that can transition to production without rework. Maintain these standards.

---

**Prepared by:** Architecture Review Bot  
**Date:** 2026  
**Applies to:** .NET 10, ASP.NET Core, Razor Pages, Clean Architecture
