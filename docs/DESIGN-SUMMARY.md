# Design Summary & Status

**Date:** 2026-06-03 · **Scope:** Expanded (API + Web UI + Persistence + Security + Cloud-ready)

---

## What's been designed

This repository is now a **comprehensive, production-shaped learning project** covering:

### ✅ Architecture & Clean Code
- **Clean Architecture** (Domain → Application → Infrastructure → API/Web) with interface-based design
- **ADR-based decision making** (11 ADRs covering architecture, persistence, auth, UI, logging, security, deployment)
- **Domain-Driven Design** principles; state machine for unit approval workflow
- **SOLID principles**, especially **Open/Closed** (new units = data only, no code changes)

### ✅ Testing & Quality
- **TDD**: unit tests (MSTest + Moq) for every layer
- **BDD**: business-readable Reqnroll feature files (including security scenarios with `@security` tag)
- **Integration**: WebApplicationFactory for end-to-end API tests
- **Performance**: BenchmarkDotNet (micro) + NBomber/k6 (load/stress/spike/soak)
- **Security testing**: SAST (SecurityCodeScan), SCA (Dependabot), DAST (OWASP ZAP), pen testing via Reqnroll

### ✅ Full-Stack Web
- **REST API** (ASP.NET Core) — stateless, public (read), authenticated (write)
- **Web UI** (Razor Pages + web components) — internal dashboard for unit submission & approval
- **Authorization** (ASP.NET Identity + policy-based + Partner API keys) — roles (Public, Employee, Partner, Admin); owner-based editing; secure API key authentication for programmatic partner access
- **Web components** (Lit/Shoelace) — no npm build required, modern, encapsulated, reusable

### ✅ Observability & Logging
- **OpenTelemetry** (traces, metrics, logs) → OTLP → **Aspire dashboard** (local review)
- **C# interceptors** (built-in stable compiler feature, .NET 9+) — source generator for logging/telemetry, clean separation
- **Structured logging** (ILogger + correlation ids)
- **Metrics** (custom Meter: conversions.count, duration, errors.count)

### ✅ Persistence & Scale
- **EF Core 9** + Repository pattern — swappable DB (SQLite / SQL Server / PostgreSQL / Oracle)
- **Caching** — L1 per-instance (immutable catalog safe), L2 Redis-ready (HybridCache)
- **Horizontal scalability** — stateless, no sticky sessions, health checks, autoscaling hooks
- **Cloud-native** — 12-factor config, container images, Aspire manifests (ACA/AKS/K8s)

### ✅ Security Baseline
- **OWASP Top 10 checklist** + threat model
- **Automated security gates** (SAST, SCA in CI)
- **BDD security scenarios** (auth, injection, CSRF, rate limiting)
- **Audit trail** (soft deletes, who/when tracking)
- **No secrets in code** (config-driven)

---

## What's documented

| Document | Scope | Status |
|----------|-------|--------|
| **README.md** | Overview + features + tech stack + getting started | ✅ |
| **docs/DESIGN-UPDATE-FULL-SCOPE.md** | Vision, architecture, solution layout, new milestones | ✅ |
| **docs/HLD.md** | High-level design, layers, request flow, quality attributes, deployment topology | ✅ Updated |
| **docs/LLD.md** | Domain model, interfaces, algorithms, API contract, logging, telemetry, caching, load tiers | ✅ Updated |
| **docs/PLAN.md** | 19 milestones (M0–M19) from scaffolding to cloud deployment, step-by-step with exit criteria | ✅ Updated |
| **docs/CODE-REVIEW-CHECKLIST.md** | Operationalized checklist for SOLID, DRY, CQRS, KISS enforcement (per-layer + red flags) | ✅ NEW |
| **docs/decision-records/** | 12 ADRs (0001–0012) covering architecture, persistence, auth, UI, interceptors, security, deployment, code quality | ✅ |

---

## Milestones (at a glance)

| Phase | Milestones | Goal |
|-------|-----------|------|
| **Foundations** | M0–M7 | Conversion engine, API, tests, observability (already detailed before scope expansion) |
| **Unit Management** | M8–M11 | State machine, persistence, auth, cloud scaffolding |
| **Web UI** | M12–M15 | Domain model, EF Core, Identity + policies, Razor Pages + web components |
| **Logging & Security** | M16–M17 | C# interceptors, SAST/SCA/DAST, OWASP checklist |
| **Scale & Deploy** | M18–M19 | Multi-DB migrations, Aspire manifests, autoscaling |

---

## Key design decisions (captured in ADRs)

| ADR | Decision | Why |
|-----|----------|-----|
| **0001** | Clean Architecture with dedicated Domain | Testability, extensibility, framework-independent logic |
| **0002** | Reqnroll + MSTest + Moq | Maintained tools, industry standard, no EOL risk |
| **0003** | Security baseline from day one | Cheap to design in, expensive to retrofit |
| **0004** | OpenTelemetry + Aspire dashboard + HybridCache | Local review without cloud, config-driven swappable backends |
| **0005** | Stateless, horizontally scalable, cloud-ready | Free horizontal scale, no redesign needed later |
| **0006** | Unit status state machine (Pending → Approved/Rejected) | Audit trail, workflow rigor, domain-driven |
| **0007** | EF Core + Repository pattern, multi-DB support | Portability, testability, industry standard ORM |
| **0008** | ASP.NET Identity + policy-based authorization | Testable, decoupled from logic, standard framework |
| **0009** | Razor Pages + web components (Lit/Shoelace) | Modern, lightweight, no npm build, industry pattern |
| **0010** | C# interceptors (built-in stable compiler feature) | Clean separation, compile-time magic, auditable `.g.cs` files |
| **0011** | Four-layer security (SAST/SCA/DAST/audit) | Comprehensive posture, automated gates, OWASP aligned |
| **0012** | **SOLID, DRY, CQRS, KISS** as guiding principles | Professional code quality, extensibility, maintainability, sustainability |
| **0013** | Partner API key authentication & programmatic access | B2B partner support without OIDC complexity; secure, revokable, stateless |
| **0014** | BDD tooling (Reqnroll in separate project) | Maintained SpecFlow fork, active community, Gherkin-compatible, MSTest integration |

---

## Testing strategy (finalized)

| Tier | Tool | Scope | Where | Gated? |
|------|------|-------|-------|--------|
| **Unit** | MSTest + Moq | Pure functions, interfaces | Domain, Application | ✅ PR |
| **Integration** | WebApplicationFactory | API contracts, EF Core | Api, Web | ✅ PR |
| **BDD** | Reqnroll | Feature + security scenarios | Bdd.Tests, Security.Tests | ✅ PR |
| **Micro-benchmark** | BenchmarkDotNet | Engine hot path | perf/ | ❌ Nightly |
| **Load/stress** | NBomber | Throughput, latency, resilience | Load.Tests | ❌ Nightly |
| **SAST** | SecurityCodeScan + NetAnalyzers | Code vulnerabilities | CI build | ✅ PR |
| **SCA** | Dependabot + `dotnet list --vulnerable` | Dependency vulnerabilities | CI gate | ✅ PR |
| **DAST** | OWASP ZAP | Runtime attack simulation | CI against staging | ✅ PR |
| **Pen test** | Reqnroll + NBomber (malformed) | Auth bypass, injection | Bdd.Tests | ✅ PR |

---

## Persistence strategy (finalized)

| Concern | Approach | Tools |
|---------|----------|-------|
| **Interface** | Repository pattern in Application | `IUnitRepository`, `IUnitApprovalRepository` |
| **ORM** | Entity Framework Core 9+ | DbContext, migrations, async |
| **Local dev** | SQLite in-memory for tests, file for CLI | Entity Framework Core's SqliteConnection |
| **Cloud** | SQL Server or PostgreSQL | Same DbContext, different migrations |
| **Schema versioning** | EF Core migrations per DB provider | `Migrations/SqlServer/`, `Migrations/PostgreSQL/` |
| **Audit** | Soft deletes + tracking fields | `CreatedBy`, `ModifiedBy`, `CreatedAt`, `ModifiedAt`, `IsDeleted` |
| **Caching** | HybridCache (L1 in-process + L2 Redis-ready) | `AddHybridCache` in Program.cs, config-driven |
| **Migration path** | Single DbContext, connection string swap | Zero code changes; environments drive DB choice |

---

## BDD & security testing (finalized)

### Reqnroll features (in `tests/UnitConverter.Bdd.Tests/Features/`)

- **Conversions.feature** — length/weight/temperature happy path
- **UnitManagement.feature** — submit/approve/reject/list workflows
- **Authorization.feature** — role-based access (employee, admin, public)
- **Security.feature** — SQL injection, XSS, CSRF, auth bypass, rate limiting
- **Errors.feature** — error handling (400/401/403/404/422 ProblemDetails)

### Security test layers

1. **SAST** — `dotnet build` fails on hardcoded secrets, weak crypto, unsafe APIs (SecurityCodeScan rules)
2. **SCA** — `dotnet list package --vulnerable` + Dependabot auto-PRs (critical/high in CI gate)
3. **DAST** — OWASP ZAP spiders + scans staging instance (warning+ triggers review)
4. **BDD** — Reqnroll scenarios with `@security` tag (explicit attack patterns tested)
5. **Pen** — NBomber with malformed payloads (fuzz-like; validates input rejection)

---

## C# interceptors (finalized)

**Approach:** Roslyn source generator, built-in compiler feature (stable since .NET 9.0.2xx SDK).

**What:** Generates interceptor methods that wrap calls to `IConvertQuantityUseCase`, `IUnitRepository`, etc., layering on logging/telemetry **without polluting business logic**.

**Setup:**
```xml
<!-- UnitConverter.Interceptors.csproj -->
<ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.9.0" />
</ItemGroup>

<!-- UnitConverter.Application.csproj (and Web.csproj) -->
<PropertyGroup>
    <InterceptorsNamespaces>$(InterceptorsNamespaces);UnitConverter.Interceptors.Generated</InterceptorsNamespaces>
</PropertyGroup>
```

**Result:** At compile time, calls to business logic are rewritten to interceptors that log + emit telemetry, then call the original. Zero runtime cost (JIT-compiled once). Auditable (`.g.cs` files committed).

---

## What you can execute next

Pick one:

### Option A: **Subagent-Driven Development (recommended)**
I create a detailed **implementation plan** (`docs/superpowers/plans/`) covering all 19 Milestones, then dispatch fresh subagents per task with two-stage review between tasks. Fast iteration, comprehensive feedback.

### Option B: **Start execution in this session**
Execute Milestone 0 (scaffold the solution, fix `UnitConverter.Domain` placement, remove stray `IUnitConverter` file, wire projects into the solution) **right now**. Once done, review, then move to M1–M7 (conversion engine + tests).

### Option C: **Pause and review**
I've designed a lot. You may want to review the ADRs, HLD, LLD, and PLAN documents first, ask clarifying questions, adjust the roadmap, or shift priorities (e.g., "security M17 should happen earlier").

---

## Repository health (current snapshot)

| File/Dir | Status |
|----------|--------|
| `UnitConverter.slnx` | Only references API project; needs update (M0) |
| `UnitConverter.API/` | Scaffolded; WeatherForecast placeholder (remove in M5) |
| `UnitConverter.Domain/` | Exists, not in solution, has `Class1.cs` placeholder (fix M0) |
| `IUnitConverter` (stray file) | Empty 0-byte file at root (delete M0) |
| `docs/` | **5 design docs + 11 ADRs** created ✅ |
| `src/`, `tests/`, `perf/` | Directories don't exist yet; create in M0 |

**Recommendation:** Start with M0 (small, scaffolding-only, 15-30 min), commit, then decide on execution path.

---

## What's next?

You asked for:
- ✅ Stress/load testing — specified (M10 with NBomber, 4 tiers)
- ✅ Cloud deployment with LB + caching — designed (M11, M18–M19 with Aspire manifests)
- ✅ Unit management + roles + admin approval — designed (M8–M15 with state machine, web UI, auth)
- ✅ Multi-DB persistence (swappable) — designed (M13, M18 with EF Core + migrations)
- ✅ Razor Pages + web components — designed (M15 with Lit/Shoelace)
- ✅ C# interceptors for logging/telemetry — designed (M16, built-in stable feature)
- ✅ Security testing + tooling — designed (M17 with SAST/SCA/DAST/OWASP/BDD)
- ✅ BDD finalization — designed (Reqnroll with feature + security tags)

**Would you like me to:**
1. Create a detailed **implementation plan** document (superpowers:writing-plans) covering all 19 Milestones?
2. Execute **Milestone 0** (scaffold) right now?
3. Review the designs first / ask clarifying questions?
