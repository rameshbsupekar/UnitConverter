# Build Plan — UnitConverter API

Status: Draft · Last updated: 2026-06-03 · Companions: [`HLD.md`](HLD.md), [`LLD.md`](LLD.md)

A milestone-by-milestone, **test-first** path. Each milestone is small, ends green
(`dotnet build` + `dotnet test` pass), and is independently reviewable/committable.

## Working rhythm (per feature)

Red → Green → Refactor, then widen:

1. Write a failing **unit test** (MSTest).
2. Write the smallest production code to pass.
3. Refactor (names, duplication, structure).
4. Add **edge cases** (boundaries, errors, round-trips).
5. Add an **integration/API test** when HTTP is involved.
6. Add a **BDD scenario** (Reqnroll) for business-visible behaviour.

## Tooling decisions (locked)

| Concern | Choice | Package(s) |
|---------|--------|-----------|
| Unit testing | **MSTest** | `Microsoft.NET.Test.Sdk`, `MSTest.TestFramework`, `MSTest.TestAdapter` |
| Mocking | **Moq** | `Moq` (pin a stable version, e.g. 4.18.x to avoid the 4.20 SponsorLink noise) |
| BDD | **Reqnroll** (maintained successor to SpecFlow — SpecFlow is end-of-life) | `Reqnroll.MsTest` |
| API integration | `WebApplicationFactory` | `Microsoft.AspNetCore.Mvc.Testing` |
| Assertions | MSTest `Assert` (default). Optional: **Shouldly** (free) — note `FluentAssertions` v8+ is commercially licensed | `Shouldly` (optional) |
| Coverage | coverlet + ReportGenerator | `coverlet.collector`, `dotnet-reportgenerator-globaltool` |
| **Observability** | **OpenTelemetry** → OTLP → **.NET Aspire dashboard** (local) | `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.AspNetCore`, `.Http`, `.Runtime`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `Aspire.Hosting.AppHost` |
| **Caching** | **HybridCache** (.NET 9+) / `IMemoryCache` + output caching | `Microsoft.Extensions.Caching.Hybrid` |
| **Micro-benchmarks** | **BenchmarkDotNet** | `BenchmarkDotNet` |
| **Load tests** | **NBomber** (C#-native) or k6 | `NBomber`, `NBomber.Http` |

> See [`decision-records/0002-bdd-and-test-tooling.md`](decision-records/0002-bdd-and-test-tooling.md)
> and [`decision-records/0004-observability-caching-performance.md`](decision-records/0004-observability-caching-performance.md).

---

## Milestone 0 — Repository & solution hygiene

Goal: clean Clean-Architecture skeleton that builds.

- [ ] Remove the stray empty `IUnitConverter` file at repo root.
- [ ] Replace `UnitConverter.Domain/Class1.cs` placeholder.
- [ ] Move projects under `src/` and rename `UnitConverter.API` → `src/UnitConverter.Api` (optional but recommended for consistency).
- [ ] Create `src/UnitConverter.Application`, `src/UnitConverter.Infrastructure`.
- [ ] Create `tests/` projects (see commands below).
- [ ] Wire project references per HLD §3 and add **all** projects to `UnitConverter.slnx`.
- [ ] Add a repo-wide `.editorconfig` (nullable, analyzers, style).
- [ ] Remove the scaffolded `WeatherForecast*` files once the first real endpoint exists.

```bash
# class libraries
dotnet new classlib -n UnitConverter.Application -o src/UnitConverter.Application
dotnet new classlib -n UnitConverter.Infrastructure -o src/UnitConverter.Infrastructure

# test projects
dotnet new mstest -n UnitConverter.Domain.Tests -o tests/UnitConverter.Domain.Tests
dotnet new mstest -n UnitConverter.Application.Tests -o tests/UnitConverter.Application.Tests
dotnet new mstest -n UnitConverter.Api.Tests -o tests/UnitConverter.Api.Tests
dotnet new mstest -n UnitConverter.Bdd.Tests -o tests/UnitConverter.Bdd.Tests

# mocking + bdd + integration + coverage
dotnet add tests/UnitConverter.Application.Tests package Moq
dotnet add tests/UnitConverter.Bdd.Tests package Reqnroll.MsTest
dotnet add tests/UnitConverter.Api.Tests package Microsoft.AspNetCore.Mvc.Testing
dotnet add tests/UnitConverter.Api.Tests package coverlet.collector

# references (inward-pointing)
dotnet add src/UnitConverter.Application reference src/UnitConverter.Domain
dotnet add src/UnitConverter.Infrastructure reference src/UnitConverter.Application
dotnet add src/UnitConverter.Api reference src/UnitConverter.Application src/UnitConverter.Infrastructure
```

**Exit:** `dotnet build` and `dotnet test` both pass (tests can be empty/placeholder).

---

## Milestone 1 — Domain: Length (TDD)

Goal: first real conversion, pure domain.

- [ ] `UnitCategory`, `Unit`, `Quantity`, `ConversionResult` records.
- [ ] `IConversionRule` + `AffineConversionRule` + `UnitConversion`.
- [ ] Domain exceptions (`UnknownUnitException`, `IncompatibleUnitsException`).
- [ ] Length units (m, km, cm, mm, inch, foot, mile).
- [ ] Tests: `1000 m → 1 km`, `1 km → 1000 m`, round-trip, incompatible-unit throws.

**Exit:** length conversions green; zero framework references in Domain.

## Milestone 2 — Domain: Weight (TDD)

- [ ] Weight units (mg, g, kg, tonne, oz, lb) with base = kg.
- [ ] Tests incl. `1 kg → 1000 g`, `1 lb → 0.45359237 kg`, round-trips.

**Exit:** weight conversions green; same rule type reused (proves extensibility).

## Milestone 3 — Domain: Temperature (TDD)

Goal: prove the **affine** (offset) path.

- [ ] Temperature units (kelvin, celsius, fahrenheit) with base = kelvin.
- [ ] Tests: `0 °C → 32 °F`, `100 °C → 212 °F`, `0 K → −273.15 °C`, round-trips with tolerance.
- [ ] Document precision strategy (see LLD §1.2).

**Exit:** all three minimum categories convert correctly from the domain alone.

## Milestone 4 — Application use cases

- [ ] `IUnitCatalog` port; `ConvertCommand`; `IConvertQuantityUseCase` + impl.
- [ ] `ListCategories` / `ListUnits` use cases.
- [ ] Tests with **Moq** for `IUnitCatalog`: success, unknown unit (404 path), cross-category (422 path).

**Exit:** use cases green; error semantics defined independent of HTTP.

## Milestone 5 — Infrastructure + API

- [ ] `InMemoryUnitCatalog` (seed from code/JSON) implementing `IUnitCatalog`.
- [ ] DI registration in `Program.cs` (composition root).
- [ ] `ConversionsController` (or minimal endpoints) + DTOs.
- [ ] Central exception → `ProblemDetails` mapping middleware.
- [ ] API tests via `WebApplicationFactory`: 200 happy path, 400/404/422 shapes, JSON contract.
- [ ] Delete `WeatherForecast*`.

**Exit:** `POST /api/conversions`, `GET /api/categories`, `GET /api/units` all work end-to-end.

## Milestone 6 — BDD feature suite

- [ ] Reqnroll project wired to MSTest; `.feature` files for length, weight, temperature.
- [ ] Step definitions drive the use case (fast) and/or the API (full-stack).
- [ ] Scenarios for error behaviour (unknown unit, incompatible units).

**Exit:** business-readable specs run in CI alongside unit/integration tests.

## Milestone 7 — Hardening & docs

- [ ] OpenAPI polish; Swagger UI in dev; example requests in `.http`.
- [ ] Validation at the edge; consistent `ProblemDetails`.
- [ ] Security baseline (ADR-0003): HTTPS, rate limiting hook, CORS for browser clients, no stack traces, correlation id in logs.
- [ ] Coverage report (coverlet + ReportGenerator) and a CI workflow (`dotnet build` + `dotnet test`).
- [ ] Keep `HLD.md` / `LLD.md` / ADRs current.

**Exit:** v1 is consumable by JS/web/mobile clients, documented, tested, and CI-gated.

## Milestone 8 — Observability & structured logging

Goal: see a request's trace, metrics, and logs **locally**. (ADR-0004)

- [ ] Add `src/UnitConverter.ServiceDefaults` (OTel + health checks + resilience registration).
- [ ] Add `src/UnitConverter.AppHost` (Aspire) so `dotnet run` launches API **and** the dashboard.
- [ ] Define `Telemetry` (`ActivitySource` + `Meter` with conversions count/duration/errors).
- [ ] Wire OpenTelemetry tracing/metrics/logging with the **OTLP exporter**.
- [ ] Structured logging via `ILogger` + `LoggerMessage` source generators; correlation id in scope + `ProblemDetails.traceId`.
- [ ] Add an API test asserting `traceId` is present on error responses.

**Exit:** running the AppHost shows a conversion's span, the metric histograms, and logs in
the Aspire dashboard. (Alt: standalone dashboard container + `OTEL_EXPORTER_OTLP_ENDPOINT`.)

## Milestone 9 — Caching

- [ ] Cache catalog reads (`GET /api/units`, `/api/categories`) via **HybridCache**/`IMemoryCache` with TTL.
- [ ] Add **output caching** + `Cache-Control`/`ETag` for catalog `GET`s.
- [ ] Tests: cache hit avoids re-reading the catalog (verify via Moq call count); TTL/eviction behaviour.
- [ ] Confirm conversions are **not** cached (by design).

**Exit:** catalog endpoints serve from cache; conversion path unchanged.

## Milestone 10 — Performance & load/stress tests

- [ ] `perf/UnitConverter.Benchmarks` (**BenchmarkDotNet**, `[MemoryDiagnoser]`) for the engine hot path; commit a baseline.
- [ ] `tests/UnitConverter.Load.Tests` (**NBomber**) for `POST /api/conversions`; assert p95/p99 latency + error-rate thresholds.
- [ ] Add **stress** (ramping inject), **spike** (instant high rate), and **soak** (long hold) simulations; watch memory/GC + `unitconverter.*` metrics in the Aspire dashboard.
- [ ] Run the load generator on a **separate host** for high-RPS runs (avoid measuring the generator).
- [ ] Document how to run them and review reports locally; keep all of these **out of the PR gate** (nightly/on-demand).

**Exit:** repeatable micro + load/stress/spike/soak benchmarks with locally reviewable reports.

## Milestone 11 — Cloud-readiness (deploy & scale)

Goal: prove the service runs stateless behind a load balancer. (ADR-0005)

- [ ] Add health/readiness endpoints (via `ServiceDefaults`) and verify LB-style probes.
- [ ] Produce a container image (`dotnet publish` / `PublishContainer`); run **2+ instances** behind a local reverse proxy (e.g. YARP/NGINX) and confirm round-robin with **no affinity**.
- [ ] Enable `UseForwardedHeaders`; confirm scheme/client-IP and `traceparent` survive the proxy.
- [ ] Confirm per-instance L1 cache correctness; document the **HybridCache + Redis (L2)** switch for a future dynamic catalog.
- [ ] (Optional) Emit deployment manifests from the Aspire `AppHost` (ACA/AKS/K8s); wire autoscaling on CPU or `unitconverter.*` metrics.

**Exit:** multi-instance, stateless deployment validated locally and ready to lift to the cloud.

---

## Milestone 12 — Domain model: unit status & approval workflow (NEW)

Goal: add the state machine for unit submission → review → approval.
**Depends on:** M0 (scaffolding) · **Unlock:** M13 (persistence) · **(ADR-0006)**

- [ ] Add `UnitStatus` enum: `Pending`, `Approved`, `Rejected`.
- [ ] Add to `Unit` record: `Status: UnitStatus`, `SubmittedBy: string` (user id), `SubmittedAt: DateTime`, `ApprovedBy: string?`, `ApprovedAt: DateTime?`, `RejectionReason: string?`.
- [ ] Create `UnitApprovalRequest` aggregate: `Id`, `UnitId`, `RequestedBy`, `RequestedAt`, `ReviewedBy?`, `ReviewedAt?`, `Decision`, `Reason?`.
- [ ] Add domain use cases: `RequestNewUnitUseCase(Unit, UserId)`, `ApproveUnitUseCase(UnitId, AdminId)`, `RejectUnitUseCase(UnitId, AdminId, reason)`.
- [ ] Write tests: state transitions, invariants (can't approve rejected units, etc.).
- [ ] Update `GetApprovedUnits()` query to filter `Status == Approved`.

**Exit:** unit workflow domain model is complete, tested, zero persistence.

## Milestone 13 — Persistence: EF Core + Repository (SQLite v1) (NEW)

Goal: persist units with the new status workflow; support multi-DB in future.
**Depends on:** M12 (domain) · **Unlock:** M14 (authorization) · **(ADR-0007)**

- [ ] Create `src/UnitConverter.Infrastructure/Data/ApplicationDbContext.cs` with DbSet<Unit>, DbSet<UnitApprovalRequest>.
- [ ] Define repository interfaces: `IUnitRepository`, `IUnitApprovalRepository` (in Application layer).
- [ ] Implement EF Core repositories in Infrastructure; all methods async.
- [ ] Create EF Core migration for SQLite: `InitialCreate` (Units, UnitApprovals tables + indices).
- [ ] Wire DbContext in DI (`AddDbContextFactory`, `AddScoped<ApplicationDbContext>`).
- [ ] Tests: repository contract via in-memory EF Core provider (`UseSqlite(":memory:")`).
- [ ] Seed test data (approved units + pending requests) in test setup.

**Exit:** units are persisted; reads filter approved only; migrations are version-controlled.

## Milestone 14 — Authorization: ASP.NET Identity + role-based policies + Partner API keys (NEW)

Goal: add roles and gates write operations; support partners via API keys.
**Depends on:** M13 (persistence) · **Unlock:** M15 (web UI) · **(ADR-0008, ADR-0013)**

- [ ] Add `Microsoft.AspNetCore.Identity` NuGet; create IdentityUser + IdentityRole DbSets.
- [ ] Create migration: identity tables.
- [ ] Seed dev roles (Public, Employee, Partner, Admin) and test users/partners in a seeding script.
- [ ] Create `Partner` entity: Id, Name, ApiKey (hashed), IsActive, CreatedAt, ExpiresAt, Metadata.
- [ ] Create migration: Partners table with indices on ApiKey (for lookup).
- [ ] Implement `ApiKeyAuthenticationHandler` (custom authentication scheme):
  - Extract Bearer token from `Authorization` header
  - Hash it, lookup in Partners table
  - Create claims: `sub` (partner id), `role` ("Partner")
  - Return `AuthenticationTicket` or fail
- [ ] Wire in `Program.cs`: `.AddAuthentication().AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>("ApiKey", ...)`.
- [ ] Define policies: `CanSubmitUnits` (Employee, Partner, Admin), `CanApproveUnits` (Admin).
- [ ] Add `[Authorize(Policy = "CanSubmitUnits")]` to `POST /api/units`.
- [ ] Add resource-based check in update handler: if `unit.SubmittedBy != User.GetUserId()`, return 403 (except Admins).
- [ ] Add `[Authorize(Policy = "CanApproveUnits")]` to `PUT /api/units/{id}/approve`.
- [ ] Tests: 401 (no auth), 403 (wrong role or not owner), 200 (correct role/owner).
- [ ] Document: how to create/revoke partner API keys via admin endpoint.

**Exit:** Employees and Partners can submit/edit own units; Admins approve; API keys authenticate partners securely.

## Milestone 15 — Web UI: Razor Pages + web components (NEW)

Goal: internal dashboard for submitting and approving units.
**Depends on:** M14 (authorization) · **Unlock:** M16 (security testing) · **(ADR-0009)**

- [ ] Create `src/UnitConverter.Web/Pages/Index.cshtml` (home).
- [ ] Create `Pages/Units/Request.cshtml/.cs` (employee form to submit new unit).
- [ ] Create `Pages/Units/Approve.cshtml/.cs` (admin dashboard: list pending, approve/reject buttons).
- [ ] Create `Pages/Units/Browse.cshtml/.cs` (public: view approved units).
- [ ] Create `wwwroot/components/unit-form.js` (web component, Lit-based, handles form binding + API call).
- [ ] Create `wwwroot/components/approval-queue.js` (web component, fetches pending units, renders queue).
- [ ] Integrate web components into Razor Pages via `<unit-form>` and `<approval-queue>` tags.
- [ ] Add authorization checks: `@authorize` tag + User.IsInRole() in code-behind.
- [ ] Tests: verify forms POST to correct endpoints; verify unauthorized users see appropriate messages.

**Exit:** employees can submit units via web form; admins see approval dashboard; public sees catalog.

## Milestone 16 — C# interceptors: source generator for logging/telemetry (NEW)

Goal: clean separation of logging from business logic via compile-time interception.
**Depends on:** M8 (observability baseline) · **Unlock:** M17 (security testing) · **(ADR-0010)**

- [ ] Create `src/UnitConverter.Interceptors/` source generator project (uses Roslyn `GetInterceptableLocation()`).
- [ ] Define `[InterceptableService]` attribute marking types to intercept.
- [ ] Write generator: for each method in `IConvertQuantityUseCase`, `IUnitRepository`, emit an interceptor method that:
  - Starts an `Activity` span
  - Calls `ConversionLog.Intercepted(...)` (structured logging)
  - Emits telemetry metrics (duration, success/error count)
  - Calls the original method
  - Returns result or propagates exception
- [ ] Generate `[InterceptsLocation(...)]` attributes pointing to each call site.
- [ ] Add `<InterceptorsNamespaces>UnitConverter.Interceptors.Generated</InterceptorsNamespaces>` to API/Web `.csproj`.
- [ ] Verify generated `.g.cs` files are correct; commit them.
- [ ] Tests: confirm interceptor fires (check logs + metrics in a unit test environment).
- [ ] Document generated code locations and how to regenerate if rules change.

**Exit:** use cases and repositories are transparently logged/metered at compile time; no `ILogger` in business logic.

## Milestone 17 — Security testing: SAST/SCA/DAST/OWASP (NEW)

Goal: comprehensive security posture with automated gates.
**Depends on:** M15 (web UI complete) · **Unlock:** M18 (multi-DB) · **(ADR-0011)**

- [ ] Enable `SecurityCodeScan.Rules` NuGet (SAST); run `dotnet build` and document violations found.
- [ ] Enable `Microsoft.CodeAnalysis.NetAnalyzers` (built-in); set rules to error on CA2000, CA2213, CA2215, CA1806.
- [ ] Create `ci/security-scan.yml` GitHub Actions workflow:
  - [ ] `dotnet list package --vulnerable` (SCA)
  - [ ] `dotnet build` with SAST rules
  - [ ] (Optional) run OWASP Dependency-Check container
  - Fail on critical/high vulnerabilities.
- [ ] Write **BDD security scenarios** (`tests/UnitConverter.Bdd.Tests/Features/Security.feature`):
  - SQL injection in unit name rejected
  - Unauthenticated POST /api/units → 401
  - Missing CSRF token → 400 (if applicable)
  - Admin can only approve (not employee) → 403
- [ ] Create `docs/security/OWASP-top-10-checklist.md` tracking coverage (A01–A10).
- [ ] Create `docs/security/threat-model.md` identifying top risks (auth bypass, data injection, DoS).
- [ ] Enable rate limiting on write endpoints (`AddRateLimiter` in `Program.cs`).
- [ ] Add `UseHsts()` for HTTPS enforcement; `X-Frame-Options`, `X-Content-Type-Options` headers.
- [ ] Tests: verify security headers present; verify rate limit triggered at threshold.

**Exit:** security is gated in CI; OWASP Top 10 baseline documented; threat model articulated.

## Milestone 18 — Multi-DB support: SQL Server & PostgreSQL migrations (NEW)

Goal: prove persistence layer is agnostic; support cloud DBs.
**Depends on:** M13 (SQLite working) · **Unlock:** M19 (cloud deploy) · **(ADR-0007)**

- [ ] Create EF Core migrations for **SQL Server**: `20240603_InitialCreate.cs`.
- [ ] Create EF Core migrations for **PostgreSQL**: `20240603_InitialCreate.cs`.
- [ ] Create `tests/DbMigrationTests.cs`: migrate each DB provider, verify schema is identical.
- [ ] Update connection string configuration to detect DB provider from environment (e.g., `DbProvider=PostgreSQL`).
- [ ] Update `Program.cs`: `.AddDbContext()` switches provider based on config.
- [ ] Document migration process: "To use PostgreSQL, set `ConnectionStrings:Database=postgresql://...` and `DbProvider=PostgreSQL`."
- [ ] Test locally: run API against SQLite, SQL Server, and PostgreSQL containers (via Docker Compose).

**Exit:** same schema works on all three DB providers; migrations are portable.

## Milestone 19 — Cloud deployment: Aspire manifests & autoscaling (NEW)

Goal: emit cloud-ready deployment specs; scale on metrics.
**Depends on:** M18 (multi-DB), M16 (interceptors logged) · **Unlock:** v1 release · **(ADR-0005, 0011)**

- [ ] Update `src/UnitConverter.AppHost` (or create if missing) to model:
  - API service (with `WithHttpHealthCheck`, `WithOtlpExporter`)
  - Postgres container (or SQL Server)
  - Aspire dashboard
- [ ] Generate manifests: `dotnet run --project src/UnitConverter.AppHost -- generate-manifests` → outputs Azure Container Apps / Kubernetes YAML.
- [ ] Configure autoscaling in manifests: scale on `http_request_duration_seconds` (p95 > 500ms) or `unitconverter.conversions.count` (rate-based).
- [ ] Smoke test in cloud (ACA / AKS / local k3s): deploy via manifest, verify API works, check Aspire sees logs/metrics.
- [ ] Document: "To deploy to Azure, run `dotnet publish --os linux --arch x64 /p:PublishProfile=Azure`, then deploy the manifest."

**Exit:** service is cloud-deployable; autoscaling policies are in place; v1 is ready for production.

---

| **Definition of Done (every milestone)** |
| - All tests green (`dotnet test`), no build warnings beyond policy. |
| - New behaviour covered by at least one unit test; user-visible behaviour by a BDD scenario. |
| - Public types have intent-revealing names; no logic leaked into controllers. |
| - **Code review checklist (ADR-0012):** SOLID (no cycles), DRY (< 5% duplication), CQRS (segregated), KISS (justified abstractions). |
| - Docs/ADRs updated if a decision changed. |
