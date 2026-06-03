# UnitConverter API & Management System

> **Read-write unit conversion service** — extensible ASP.NET Core REST API + internal web dashboard for employees to submit and admins to verify new units — built with Clean Architecture, TDD, BDD, and designed for cloud scale.

UnitConverter is a **learning-grade-but-production-shaped** system with two surfaces:
- **Public REST API** — stateless, reads approved units, converts between them
- **Internal web app** (Razor Pages + web components) — employees submit new units, admins review & approve

The conversion logic lives in a framework-independent domain, exercised by automated tests from
day one (TDD + BDD). Both API and web UI share the same domain and persistence layer (swappable
across SQLite / SQL Server / PostgreSQL). Security, logging, and telemetry are baked in (C#
interceptors for clean separation, OpenTelemetry + Aspire dashboard for local review).

---

## Repository description (for Git / GitHub "About")

Use this as the GitHub **About** blurb (short, ≤ 120 chars, no trailing period — GitHub style):

```
Extensible unit converter REST API in ASP.NET Core — Clean Architecture, TDD & BDD
```

Suggested **topics/tags**: `aspnetcore` `rest-api` `clean-architecture` `tdd` `bdd`
`reqnroll` `mstest` `csharp` `dotnet` `unit-conversion`

Longer one-line variant (for `<description>` in docs or NuGet):

> UnitConverter API — extensible ASP.NET Core REST API for converting values across Length,
> Temperature, Weight, and future unit categories, built with Clean Architecture, TDD, and BDD.

---

## Initial supported categories

| Category    | Base unit | Example units                          | Conversion style          |
|-------------|-----------|----------------------------------------|---------------------------|
| Length      | metre     | mm, cm, m, km, inch, foot, mile        | Linear factor             |
| Weight/Mass | kilogram  | mg, g, kg, tonne, ounce, pound         | Linear factor             |
| Temperature | kelvin    | Celsius, Fahrenheit, Kelvin            | Affine (offset + scale)   |

> The model supports **any number** of categories and units. Adding a new unit is a **form submission + admin approval**, then it's live in the API (no code change).

## Features

| Aspect | v1 capabilities |
|--------|-----------------|
| **API** | `POST /api/conversions` (convert), `GET /api/units` (list approved), `POST /api/units` (submit), `PUT /api/units/{id}/approve` (admin) |
| **Web UI** | Razor Pages + web components; employee form, admin approval queue, public catalog view |
| **Authorization** | ASP.NET Identity roles (Public, Employee, Partner, Admin); policy-based `[Authorize]` gates; Owner-based editing (employees/partners can only edit own submissions); Partner API keys (Bearer token) for programmatic access |
| **Persistence** | EF Core + Repository pattern; SQLite locally, SQL Server / PostgreSQL in cloud |
| **Logging & telemetry** | C# interceptors (source generator) + OpenTelemetry traces/metrics → Aspire dashboard locally |
| **Security** | OWASP Top 10 baseline, dependency scanning, pen testing (DAST), BDD security scenarios |
| **Cloud-ready** | stateless, horizontally scalable behind LB, health checks, multi-DB support, Aspire manifests for ACA/AKS/K8s |

## Architecture goals

- Keep **domain logic independent** of ASP.NET Core (testable without a web host).
- Support **future unit categories** without modifying API endpoints.
- Use **automated tests from the start** (unit, integration, and behaviour specs).
- Provide **REST endpoints** consumable by web, JavaScript, mobile, and desktop clients.
- **Enforce professional code practices** — SOLID (extensibility), DRY (maintainability), CQRS (clarity), KISS (sustainability).

## Tech stack

- **Runtime / API:** ASP.NET Core (`net10.0`), OpenAPI
- **Web UI:** Razor Pages + modern web components (Lit/Shoelace, no npm build required)
- **Authorization:** ASP.NET Core Identity + policy-based
- **Architecture:** Clean Architecture (Domain → Application → Infrastructure → Api/Web)
- **Unit tests:** MSTest + **Moq**
- **Integration tests:** `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`)
- **BDD:** **Reqnroll** (maintained successor to SpecFlow) + `Reqnroll.MsTest` (security + feature scenarios)
- **Coverage:** coverlet + ReportGenerator
- **Observability:** OpenTelemetry (traces/metrics/logs) → OTLP → **.NET Aspire dashboard** (local review)
- **Logging:** **C# interceptors** (built-in stable compiler feature, .NET 9+, source generator) + structured `ILogger` + correlation ids
- **Caching:** `HybridCache`/`IMemoryCache` + output caching for approved catalog (L2 Redis ready)
- **Performance tests:** BenchmarkDotNet (micro) + NBomber/k6 (load/stress/spike/soak)
- **Security:** SAST (SecurityCodeScan + NetAnalyzers) + SCA (Dependabot) + DAST (OWASP ZAP) + BDD security scenarios
- **Persistence:** EF Core 9+ with Repository pattern; swappable DB (SQLite / SQL Server / PostgreSQL / Oracle)

## Documentation

| Doc | Purpose |
|-----|---------|
| [`docs/HLD.md`](docs/HLD.md) | High-Level Design — context, layers, request flow, quality attributes |
| [`docs/LLD.md`](docs/LLD.md) | Low-Level Design — types, interfaces, conversion algorithms, API contract |
| [`docs/PLAN.md`](docs/PLAN.md) | Milestone-by-milestone TDD/BDD build path |
| [`docs/decision-records/`](docs/decision-records/) | Architecture Decision Records (ADRs) |

## Getting started

```bash
# Restore & build
dotnet build

# Run the API
dotnet run --project src/UnitConverter.Api

# Run all tests
dotnet test
```

> The target solution layout (`src/` + `tests/`) is described in
> [`docs/HLD.md`](docs/HLD.md). The repository currently contains the scaffolded
> `UnitConverter.API` and an empty `UnitConverter.Domain`; see
> [`docs/PLAN.md`](docs/PLAN.md) Milestone 0 for the migration steps.

## License

MIT / Apache 2.0 open-source (.NET Foundation / community libraries).
See [`DEPENDENCIES.md`](DEPENDENCIES.md) for full list.
