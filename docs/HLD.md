# High-Level Design (HLD) — UnitConverter API

Status: Draft · Owner: Engineering · Last updated: 2026-06-03

## 1. Purpose & scope

Build a production-shaped **unit conversion** service that:

- supports **Length, Temperature, Weight** at minimum, and **any number** of future categories;
- exposes conversion over a **REST API** usable by JavaScript/web, mobile, desktop, and automation clients;
- is **testable from day one** via TDD (unit) and BDD (behaviour specs);
- follows **Clean Architecture** so the domain has zero framework dependencies.

Out of scope (for v1): persistence of conversion history, user accounts, currency/FX
(rates change over time and need an external source).

## 2. Context diagram (C4 level 1)

```
            ┌───────────────────────────────────────────────┐
  Clients   │                                               │
  ────────► │            UnitConverter API                  │
  Web SPA   │   (ASP.NET Core REST, OpenAPI/Swagger)        │
  Mobile    │                                               │
  Desktop   │   ┌─────────────────────────────────────┐    │
  Scripts   │   │  Conversion engine (Domain + App)   │    │
            │   └─────────────────────────────────────┘    │
            │            ▲                                  │
            │            │ reads unit catalog               │
            │   ┌────────┴───────────┐                      │
            │   │ Unit catalog (in-  │  (config/JSON now,   │
            │   │ memory / config)   │   DB later if needed)│
            │   └────────────────────┘                      │
            └───────────────────────────────────────────────┘
```

There are **no required external systems** for v1. The unit catalog is data the service
owns; it can start in code/config and move to a database only if a real need appears (YAGNI).

## 3. Architecture style — Clean Architecture

Dependencies point **inward only**. The Domain knows nothing about the web, DI, or JSON.

```
            ┌──────────────────────────────────────────────┐
            │                 UnitConverter.Api             │  ← controllers/endpoints,
            │  (ASP.NET Core, OpenAPI, middleware, DI)      │    middleware, problem details
            │   depends on ▼                                │
            │  ┌────────────────────────────────────────┐  │
            │  │           UnitConverter.Application      │ │  ← use cases, DTOs,
            │  │  (orchestration, validation, ports)      │ │    abstractions (ports)
            │  │            depends on ▼                  │  │
            │  │   ┌──────────────────────────────────┐   │ │
            │  │   │       UnitConverter.Domain        │   │ │  ← entities, value objects,
            │  │   │  (entities, rules, invariants)    │   │ │    conversion rules — NO deps
            │  │   └──────────────────────────────────┘   │ │
            │  └────────────────────────────────────────┘  │
            │                                               │
            │  UnitConverter.Infrastructure ──implements──► │  ← unit catalog source,
            │  (ports from Application; registered in API)  │    config, optional persistence
            └──────────────────────────────────────────────┘
```

| Layer | Responsibility | Depends on | Must NOT reference |
|-------|----------------|------------|--------------------|
| **Domain** | Units, quantities, conversion rules, invariants | nothing | ASP.NET, DI, JSON, EF |
| **Application** | Use cases (Convert, ListUnits), DTOs, validation, **ports** (interfaces) | Domain | ASP.NET, EF |
| **Infrastructure** | Implements ports: unit catalog provider, config, optional persistence | Application, Domain | — |
| **Api** | HTTP endpoints, model binding, error mapping, OpenAPI, composition root (DI) | Application, Infrastructure | — |

**Core rule:** conversion logic never lives in a controller. The API binds input,
delegates to an Application use case, and maps the result to HTTP.

## 4. Logical components

- **Unit catalog** — the set of categories and units with their conversion metadata (approved units only, public-facing).
- **Unit submission & approval workflow** — employees/partners submit new units, admins review and approve/reject (state machine: Pending → Approved/Rejected).
- **Conversion engine** — selects the correct `IConversionRule` for a `from`/`to` pair and applies it.
- **Authorization & roles** — Public (unauthenticated), Employee (internal, submit/edit own), Partner (external, submit/edit own via API key), Admin (approve/edit any).
- **Use cases** — `ConvertQuantity`, `ListCategories`, `ListUnits`, `SubmitUnit`, `EditUnit`, `ApproveUnit`, `RejectUnit`.
- **API endpoints** — thin HTTP adapters over the use cases; routes protected by policies (e.g., `[Authorize(Policy = "CanApproveUnits")]`).
- **Web UI** — Razor Pages + web components for employee/partner submission and admin approval dashboard.
- **Cross-cutting** — validation, exception-to-ProblemDetails mapping, OpenAPI, C# interceptor-based logging/telemetry, rate limiting, OWASP baseline.

## 5. Primary request flow — `POST /api/conversions`

```
Client → API endpoint
       → validate request shape (ASP.NET model binding + validator)
       → ConvertQuantityUseCase(request)
             → resolve from/to Units from Unit catalog (Application port)
             → guard: same category? known units?
             → select IConversionRule (linear | affine | formula)
             → compute converted value (Domain)
       ← ConversionResult
       ← map to 200 OK ConversionResponse
       (or 400/404/422 ProblemDetails on invalid input/unknown unit/incompatible category)
```

## 6. Public API surface (v1)

| Method | Route | Purpose | Success | Errors |
|--------|-------|---------|---------|--------|
| `POST` | `/api/conversions` | Convert a value between two units | `200` | `400` malformed, `404` unknown unit, `422` incompatible units |
| `GET`  | `/api/categories` | List unit categories | `200` | — |
| `GET`  | `/api/units` | List units (optional `?category=length`) | `200` | `404` unknown category |

Conventions: JSON, kebab/lowercase unit symbols, RFC 7807 `ProblemDetails` for errors,
OpenAPI document published in all environments (Swagger UI in dev).

## 7. Quality attributes (how the design meets them)

| Attribute | Approach |
|-----------|----------|
| **Extensibility** | New units/categories = data + a registered rule; no endpoint changes (Open/Closed). |
| **Testability** | Pure domain; ports mocked with Moq; integration via `WebApplicationFactory`; behaviour via Reqnroll. |
| **Correctness** | `decimal` for money-grade precision; affine model for temperature; round-trip tests. |
| **Maintainability** | Clean Architecture boundaries; small cohesive types; EditorConfig; nullable reference types on. |
| **Security** | Validate at the edge, never echo stack traces, HTTPS, OpenAPI as contract, auth/rate-limit hooks ready (see ADR-0003 & LLD §8). |
| **Observability** | OpenTelemetry traces/metrics/logs exported via OTLP to a **local .NET Aspire dashboard**; structured logging + correlation id (see ADR-0004 & §10). |
| **Performance** | `decimal` math is cheap; **catalog reads cached** (not the arithmetic); regressions caught by **BenchmarkDotNet** + **NBomber/k6** load tests (ADR-0004). |

## 8. Target solution layout

```
UnitConverter.slnx
├── src
│   ├── UnitConverter.Domain          (class lib — no deps)
│   ├── UnitConverter.Application     (class lib — refs Domain)
│   ├── UnitConverter.Infrastructure  (class lib — refs Application)
│   ├── UnitConverter.Api             (web — refs Application + Infrastructure)
│   ├── UnitConverter.ServiceDefaults (Aspire — OTel + health + resilience wiring)
│   └── UnitConverter.AppHost         (Aspire orchestration; runs API + dashboard) [optional]
├── tests
│   ├── UnitConverter.Domain.Tests        (MSTest)
│   ├── UnitConverter.Application.Tests    (MSTest + Moq)
│   ├── UnitConverter.Api.Tests            (MSTest + WebApplicationFactory)
│   ├── UnitConverter.Bdd.Tests            (Reqnroll + MSTest)
│   └── UnitConverter.Load.Tests           (NBomber load/throughput)
├── perf
│   └── UnitConverter.Benchmarks           (BenchmarkDotNet micro-benchmarks)
└── docs
    ├── HLD.md  LLD.md  PLAN.md
    └── decision-records/
```

> Migration note: today the repo has `UnitConverter.API` (scaffolded) and an empty
> `UnitConverter.Domain` not yet in the solution. `docs/PLAN.md` Milestone 0 covers the move.

## 9. Key decisions (see ADRs)

- **ADR-0001** — Clean Architecture with a dedicated Domain.
- **ADR-0002** — Reqnroll for BDD, MSTest + Moq for unit tests.
- **ADR-0003** — Security & error-handling baseline from the start.
- **ADR-0004** — Observability (OpenTelemetry + local Aspire dashboard), logging, caching, performance tests.
- **ADR-0005** — Stateless, horizontally scalable, cloud-ready deployment.
- **ADR-0012** — **SOLID, DRY, CQRS, KISS as guiding principles** (enforcement checklist for code quality).

## 10. Observability, logging, caching & performance

Detailed in [`LLD.md`](LLD.md) §9–§11 and [ADR-0004](decision-records/0004-observability-caching-performance.md).

```
                     OTLP (gRPC/http)
  UnitConverter.Api ───────────────────►  .NET Aspire dashboard  (local: traces · metrics · logs)
   │  OpenTelemetry SDK                         ▲
   │  ├─ traces   (ActivitySource "UnitConverter")
   │  ├─ metrics  (Meter "UnitConverter": conversions.count / duration / errors.count)
   │  └─ logs     (ILogger → OTel log pipeline, correlation id)
   │
   ├─ HybridCache / IMemoryCache  → catalog reads (units, categories)  [TTL + eviction]
   └─ ServiceDefaults             → health checks, resilience, OTel registration
```

- **Local review (primary):** run via the Aspire **AppHost** (or the standalone dashboard
  container) and open the dashboard to inspect a conversion request's trace, the metrics
  histograms, and structured logs — no cloud account needed.
- **Swap-friendly:** the same OTLP exporter can target Jaeger/Prometheus+Grafana/Seq or a
  cloud APM in deployed environments via configuration only.
- **Caching scope:** stable **catalog reads** are cached; **conversion arithmetic is not**
  (cheap; caching would add staleness/memory for no gain).
- **Performance tiers:** **BenchmarkDotNet** for the engine hot path; **NBomber/k6** for HTTP
  load. Both run out-of-band (nightly/on-demand), not in the PR gate.

## 11. Deployment & scalability (cloud-ready)

The design targets **horizontal scale behind a load balancer** with no rework. The enabling
property is that the API is **stateless** (conversion is pure; no session/affinity).
See [ADR-0005](decision-records/0005-stateless-cloud-ready-deployment.md).

```
                 ┌───────────── Load balancer / ingress ─────────────┐
   clients ────► │  TLS termination · health probes · traceparent     │
                 └───────────────────────────────────────────────────┘
                     │            │            │     (no sticky sessions)
                 ┌───▼───┐    ┌───▼───┐    ┌───▼───┐
                 │ API #1 │    │ API #2 │    │ API #N │   ← identical stateless instances
                 │ L1 cache│   │ L1 cache│   │ L1 cache│  ← per-instance catalog cache (immutable → safe)
                 └───┬───┘    └───┬───┘    └───┬───┘
                     └────────────┼────────────┘
                          (optional) L2 distributed cache (Redis) via HybridCache
                          OTLP ───► telemetry backend (Aspire local / Grafana / APM in cloud)
```

| Concern | How the design supports it |
|---------|----------------------------|
| **Horizontal scaling** | Stateless API → add instances freely; round-robin LB, no sticky sessions. |
| **Health checks** | `ServiceDefaults` exposes liveness/readiness → LB probes & K8s `liveness`/`readiness`. |
| **Caching at scale** | Per-instance L1 cache is correct for the **immutable** catalog; if it becomes dynamic, `HybridCache` adds **L2 (Redis)** by config — no redesign. |
| **Config-driven (12-factor)** | OTLP endpoint, TTLs, rate limits via configuration → one image, many environments. |
| **Containerization & deploy** | `dotnet publish` container images; Aspire `AppHost` models topology, can emit ACA/AKS/K8s manifests. |
| **Autoscaling** | Emitted metrics (RPS, duration) drive CPU/custom-metric HPA/KEDA. |
| **Edge/CDN** | `GET` catalog endpoints carry `Cache-Control`/`ETag`; `POST /conversions` stays uncached. |
| **Behind a proxy** | Enable `UseForwardedHeaders` so scheme/client-IP in logs/traces are correct; LB must pass W3C `traceparent`. |

> **Load/stress testing** validates these claims before cloud rollout — see
> [`LLD.md`](LLD.md) §12 (load, stress, spike, soak tiers).
