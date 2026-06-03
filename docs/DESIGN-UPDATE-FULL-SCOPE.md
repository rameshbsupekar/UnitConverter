# Design Update: Unit Management + Web UI + Persistence + Security

Status: Draft · Last updated: 2026-06-03

## Vision (updated from v1 read-only)

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Internal Web App (Razor Pages)                   │
│  ├─ Employees/partners: Request new units (form)                    │
│  ├─ Admins: Review, verify, approve (queue dashboard)               │
│  └─ Public: Browse approved units (catalog view)                    │
├─────────────────────────────────────────────────────────────────────┤
│                    Public REST API (unchanged)                      │
│  ├─ GET /api/conversions (public)                                   │
│  ├─ GET /api/units (approved only)                                  │
│  ├─ POST /api/units (authed: employees/admins only)                 │
│  ├─ PUT /api/units/{id}/approve (authed: admins only)               │
│  └─ DELETE /api/units/{id}/reject (authed: admins only)             │
├─────────────────────────────────────────────────────────────────────┤
│            Shared Domain + Persistence (swappable DB)               │
│  ├─ Domain (interface-based, no EF Core references)                 │
│  ├─ Repository pattern (IUnitRepository, IApprovalRepository)       │
│  ├─ EF Core DbContext (SqlServer / PostgreSQL / SQLite)             │
│  └─ Migrations per DB provider                                      │
├─────────────────────────────────────────────────────────────────────┤
│          Cross-cutting (logging, telemetry, security)               │
│  ├─ C# interceptors (source generator) for logging/analytics        │
│  ├─ OpenTelemetry metrics + Aspire dashboard                        │
│  ├─ Security: OWASP Top 10 baseline, dependency scanning, authn/z   │
│  └─ Static code analysis (Roslyn + Sonar / Microsoft.CodeAnalysis)  │
└─────────────────────────────────────────────────────────────────────┘
```

## Key architectural decisions

### 1. **Unit Management Workflow**

Units start as **pending** (unverified requests), can be **approved** by admins, and move to **public** (discoverable). Rejected units go to a **rejected** state.

```
User (employee) submits ──► Pending ──► Admin review
                                           ├─► Approved → visible in API
                                           └─► Rejected → archived
```

Domain aggregates:
- `Unit` — now has a `Status` (Pending, Approved, Rejected) and `SubmittedBy` (user id).
- `UnitApprovalRequest` — tracks the review workflow (submitted, reviewed-by, decision timestamp, reason).

### 2. **Persistence Layer**

**Strategy:** Repository pattern + EF Core, with swappable DB support.

- Interface (`IUnitRepository`, `IUnitApprovalRepository`) → implementation per DB
- DbContext abstracts the model; migrations per DB provider
- **Initially:** SQLite (local dev); SQL Server / PostgreSQL (cloud)
- **Tech:** EF Core 9.0+, async/await all the way, soft-deletes for audit

**Migration story:**
```
├── src/UnitConverter.Infrastructure/Data/
│   ├── ApplicationDbContext.cs             (model definitions)
│   ├── Repositories/
│   │   ├── IUnitRepository.cs              (interface)
│   │   ├── UnitRepository.cs               (EF Core impl)
│   │   └── IUnitApprovalRepository.cs
│   └── Migrations/
│       ├── SqlServer/
│       │   └── ***.cs (EF migrations)
│       ├── PostgreSQL/
│       └── SQLite/
```

### 3. **Authorization & Roles**

Three roles:
- **Public** (unauthenticated) — read approved units, convert
- **Employee** — submit new units
- **Admin** — approve/reject submissions

Use **ASP.NET Core policy-based authorization** (not attribute-based; more testable).

```csharp
services.AddAuthorizationBuilder()
    .AddPolicy("CanSubmitUnits", policy => policy.RequireRole("Employee", "Admin"))
    .AddPolicy("CanApproveUnits", policy => policy.RequireRole("Admin"));
```

Authentication: ASP.NET Core Identity or OIDC (AAD/Keycloak).

### 4. **Web UI — Razor Pages + Web Components**

**Why not Blazor or Angular?**
- Blazor → C# in browser (WASM), overkill for this form-driven workflow; adds build complexity.
- Angular → heavier SPA; web components are lighter, modern, and don't require separate npm build.

**Approach:** Razor Pages + **web components** (custom elements defined in small JS files, encapsulated, reusable):

```
├── src/UnitConverter.Web/
│   ├── Pages/
│   │   ├── Index.cshtml / Index.cshtml.cs        (home)
│   │   ├── Units/
│   │   │   ├── Request.cshtml/.cs                (employee: submit new)
│   │   │   ├── Approve.cshtml/.cs                (admin: review queue)
│   │   │   └── Browse.cshtml/.cs                 (public: list approved)
│   │   └── Shared/_Layout.cshtml
│   ├── wwwroot/components/
│   │   ├── unit-form.js                          (web component)
│   │   ├── approval-queue.js                      (web component)
│   │   └── unit-card.js                           (web component)
│   └── appsettings.json
```

**Web components libraries:** Lit (lightweight, zero-dep) or Shoelace (styled, accessible).

### 5. **C# Interceptors for Logging/Telemetry**

**Clarification:** C# interceptors are compile-time call-site rewriting via a Roslyn source generator. This is *not* a runtime wrapper (like the Decorator I suggested earlier).

**Use case:** Generate interceptors that log entry/exit and emit telemetry for every method in `IConvertQuantityUseCase`, `IUnitRepository`, etc., without adding logging code to business logic.

**Trade-off:** requires maintaining a **source generator** (more moving parts) but pays off if logging rules change (regenerate, no code changes).

**Setup:**
```
├── src/UnitConverter.Interceptors/          (NEW: source generator project)
│   ├── UnitConverterInterceptorGenerator.cs (generates [InterceptsLocation] methods)
│   └── UnitConverter.Interceptors.csproj     (set GenerateDocumentationFile=true)
├── src/UnitConverter.Application/
│   └── *.csproj                             (add <InterceptorsNamespaces>)
```

Regenerator emits interceptor methods that replace calls to logging methods with enhanced telemetry.

### 6. **Security Testing & Tooling**

**Layers:**
1. **Static code analysis** — `Microsoft.CodeAnalysis` (Roslyn), Sonar/Qube rules
2. **Dependency scanning** — `dotnet list package --vulnerable`, OWASP Dependency-Check NuGet
3. **Pen testing / fuzz** — OWASP ZAP (spidering + scanning), NBomber with malformed payloads
4. **Compliance** — OWASP Top 10 checklist, CWE coverage

**Tools / NuGet packages:**
- `SecurityCodeScan.Rules` — Roslyn analyzer for injection, hardcoded secrets, weak crypto
- `NDepend` — architecture + complexity metrics, dead code, violations
- `Microsoft.CodeAnalysis.NetAnalyzers` — built-in (high confidence)
- `GitHub.Dependabot` (free, CI integrated) or `Snyk` (cloud)
- **Aspire dashboard** — watch for error spikes (potential attacks)

**BDD for security:** `Given` attacker tries to access admin unit, `When` they POST without auth, `Then` 401 Unauthorized (Reqnroll step definitions).

### 7. **BDD Tools Finalization**

- **Reqnroll** (not SpecFlow; EOL) + `Reqnroll.MsTest`
- Feature files in `tests/UnitConverter.Bdd.Tests/Features/`
- Steps organized by domain (Conversions, Units, Approval, Security)
- **Step organization:** tag by layer (API vs use case)
  ```gherkin
  @api @security
  Scenario: Unauthenticated user cannot submit units
    Given I am not authenticated
    When I POST to /api/units with a new unit
    Then the response status is 401
  ```

---

## Solution Layout (expanded)

```
UnitConverter.slnx
├── src
│   ├── UnitConverter.Domain
│   ├── UnitConverter.Application
│   ├── UnitConverter.Infrastructure
│   ├── UnitConverter.Api
│   ├── UnitConverter.Web                    (NEW: Razor Pages)
│   ├── UnitConverter.ServiceDefaults        (Aspire OTel + health)
│   ├── UnitConverter.AppHost                (Aspire orchestration) [optional]
│   └── UnitConverter.Interceptors           (NEW: source generator for logging)
├── tests
│   ├── UnitConverter.Domain.Tests
│   ├── UnitConverter.Application.Tests
│   ├── UnitConverter.Api.Tests
│   ├── UnitConverter.Bdd.Tests              (updated with auth/approval scenarios)
│   ├── UnitConverter.Load.Tests
│   ├── UnitConverter.Security.Tests         (NEW: OWASP/pen-test scenarios)
│   └── UnitConverter.Web.Tests              (NEW: Razor Pages/component tests)
├── perf
│   └── UnitConverter.Benchmarks
├── docs
│   ├── HLD.md  LLD.md  PLAN.md
│   ├── decision-records/
│   │   ├── 0001-clean-architecture.md
│   │   ├── 0002-bdd-and-test-tooling.md
│   │   ├── 0003-security-and-error-baseline.md
│   │   ├── 0004-observability-caching-performance.md
│   │   ├── 0005-stateless-cloud-ready-deployment.md
│   │   ├── 0006-unit-management-workflow.md      (NEW)
│   │   ├── 0007-persistence-and-repository.md    (NEW)
│   │   ├── 0008-authorization-and-roles.md       (NEW)
│   │   ├── 0009-razor-pages-and-web-components.md (NEW)
│   │   ├── 0010-c-sharp-interceptors.md          (NEW)
│   │   └── 0011-security-testing-and-tooling.md  (NEW)
│   └── security/
│       ├── OWASP-top-10-checklist.md             (NEW)
│       └── threat-model.md                        (NEW)
└── .github/workflows/
    ├── build.yml
    └── security.yml                              (NEW: dependency check + SAST)
```

---

## New Milestones (Sketched)

| # | Milestone | Depends on | Scope |
|---|-----------|-----------|-------|
| 0–7 | Original (conversion + API + perf + observability) | — | ✅ Exists |
| 8 | Domain model: unit status + approval workflow | M0 | Add `Status`, `SubmittedBy`, `UnitApprovalRequest` |
| 9 | Persistence: EF Core + Repository (SQLite) | M8 | DbContext, migrations, repositories |
| 10 | Authorization: roles + policies | M9 | ASP.NET Identity + policy-based authz |
| 11 | API: unit submission + approval endpoints | M10 | POST/PUT/DELETE with auth guards |
| 12 | Web UI: Razor Pages + web components | M11 | Forms, approval queue, catalog view |
| 13 | C# interceptors: source generator setup | M7 | Generate logging interceptors; integrate |
| 14 | Security testing: OWASP + dependency scan | M11 | Static analyzers, pen-test scripts, CI gate |
| 15 | Multi-DB support: migrate SQLite → PostgreSQL/SQL Server | M9 | Add migrations; swap connection string |
| 16 | Cloud deploy: multi-instance, Aspire manifests, autoscaling | M15 | Replicate M11 with added DB tier |

---

## Key design principles (unchanged from v1, extended)

1. **Interface-based domain** — nothing in Domain references EF Core or web framework.
2. **Repository pattern** — persistence is swappable; test with in-memory impl.
3. **Authorization at the boundary** — ASP.NET policies in controllers; domain stays pure.
4. **Logging via interceptors** — no `ILogger` in business logic; generated at compile time.
5. **Horizontal scale** — stateless, no per-instance state; L1 cache safe (immutable catalog).
6. **Security by design** — OWASP baseline, dependency scanning, pen testing in CI.
7. **Cloud-ready** — 12-factor config, container images, OTLP exporters, multi-DB support.

