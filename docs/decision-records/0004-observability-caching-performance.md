# ADR-0004 — Observability, logging, caching, and performance testing

- Status: Accepted
- Date: 2026-06-03

## Context

We want, from early on: **structured logging**, **caching**, **observability telemetry**
(traces, metrics, logs), and **performance tests** — with the explicit requirement to
**review telemetry locally** without standing up cloud infrastructure. The repository path
(`TestAspire`) signals interest in **.NET Aspire**, which provides a local dashboard for
exactly this.

## Decision

### Observability — OpenTelemetry + local dashboard

- Instrument the API with **OpenTelemetry** (`OpenTelemetry.Extensions.Hosting` +
  ASP.NET Core / HttpClient / Runtime instrumentation) for **traces, metrics, and logs**.
- Export via **OTLP** to the **.NET Aspire dashboard** for local review (logs, distributed
  traces, and metrics in one UI). Add an **AppHost** + **ServiceDefaults** project, or run
  the **standalone Aspire dashboard** container and point `OTEL_EXPORTER_OTLP_ENDPOINT` at it.
- Define domain telemetry primitives:
  - a custom **`ActivitySource`** (`UnitConverter`) for conversion spans;
  - a **`Meter`** with metrics: `unitconverter.conversions.count` (counter, tagged by
    `from`/`to`/`category`), `unitconverter.conversion.duration` (histogram),
    `unitconverter.conversions.errors.count` (counter).
- **Correlation id** on every request, propagated to logs and traces (W3C `traceparent`).

### Logging

- **`ILogger<T>`** structured logging with **scopes** and **named properties** (no string
  concatenation). Use `LoggerMessage` source generators for hot-path log statements.
- **Scrub PII/secrets**; log full error detail server-side, surface only a correlation id to clients.

### Caching

- Cache **catalog reads** (`GET /api/units`, `/api/categories`), which are stable and the
  main reuse opportunity — via **`HybridCache`** (.NET 9+) or `IMemoryCache`, plus
  **OpenAPI/output caching** where appropriate, with explicit **TTL** and **eviction**.
- **Do NOT cache individual conversion results** by default: conversions are cheap pure
  arithmetic, so a per-result cache adds memory + staleness risk for no real gain (YAGNI).
  Revisit only if a profiler shows a hot path that warrants it.

### Performance testing — two tiers

- **Micro-benchmarks:** **BenchmarkDotNet** project (`perf/UnitConverter.Benchmarks`) for the
  conversion hot path (allocations + time). Run on demand / nightly, not in PR gate.
- **Load / throughput:** **NBomber** (C#-native, lives in `tests/UnitConverter.Load.Tests`) or
  **k6** scripts against a running instance, asserting latency percentiles and error rate.

## Rationale

- **Local-first review:** the Aspire dashboard satisfies "get data locally to review" with
  near-zero setup; OTLP keeps us vendor-neutral (swap to Jaeger/Prometheus/Grafana/Seq or a
  cloud backend later with config only).
- **Right caching target:** caching the catalog (not the math) reflects where cost actually is.
- **Two perf tiers:** micro-benchmarks catch regressions in the engine; load tests validate
  the HTTP stack under concurrency. Keeping them out of the PR gate keeps CI fast/deterministic.

## Consequences

- Extra projects: `ServiceDefaults` (+ optional `AppHost`), `perf/UnitConverter.Benchmarks`,
  `tests/UnitConverter.Load.Tests`.
- A small amount of telemetry wiring in `Program.cs` (centralized in ServiceDefaults).
- Perf tooling pinned and documented; benchmarks excluded from the default `dotnet test` run.

## Alternatives considered

- **Application Insights / cloud APM only** — rejected as the *default* local-review path;
  remains a valid additional OTLP exporter for deployed environments.
- **Seq for logs + Jaeger for traces (separate tools)** — good, but two UIs vs the single
  Aspire dashboard; documented as an alternative.
- **Cache every conversion result** — rejected (YAGNI; arithmetic is cheaper than a cache lookup).
