# Low-Level Design (LLD) — UnitConverter API

Status: Draft · Last updated: 2026-06-03 · Companion to [`HLD.md`](HLD.md)

This document specifies the concrete types, contracts, algorithms, and API shapes.
Code shown is **design intent** (illustrative), not final source.

## 1. Domain model

The domain is pure C# with no framework dependencies. Use `record` for immutable
value-like data; throw domain exceptions for broken invariants.

**See [`DOMAIN-KNOWLEDGE.md`](../DOMAIN-KNOWLEDGE.md) for the physics/math behind units and conversions.**

```csharp
namespace UnitConverter.Domain;

// A category groups compatible units (e.g. all lengths).
public sealed record UnitCategory(string Name);

// A unit belongs to exactly one category and carries the metadata needed to convert.
public sealed record Unit(
    string Symbol,        // canonical, lowercase, unique within a category: "m", "km", "celsius"
    string Name,          // human friendly: "metre", "kilometre"
    UnitCategory Category);

// A value paired with its unit. The thing the API actually converts.
public sealed record Quantity(decimal Value, Unit Unit);

// Outcome of a conversion (kept separate from the request DTO).
public sealed record ConversionResult(decimal Value, Unit Unit);
```

### 1.1 Conversion rules (Strategy pattern)

Two real forces exist, so we model two rule kinds (and leave room for a third):

- **Linear factor** — length, weight: `base = value * factor`, then `target = base / targetFactor`.
- **Affine** — temperature: `base = value * scale + offset` (offset ≠ 0).
- **Formula** — escape hatch for anything non-affine in the future.

```csharp
public interface IConversionRule
{
    bool CanConvert(Unit from, Unit to);
    decimal Convert(decimal value, Unit from, Unit to);
}
```

Both linear and affine reduce to "convert to the category base unit, then to the target".
We capture that per-unit as a **affine pair** `(scaleToBase, offsetToBase)`; linear units
simply have `offset = 0`.

```csharp
// Stored alongside each unit in the catalog (Infrastructure), consumed by the rule.
public sealed record UnitConversion(decimal ScaleToBase, decimal OffsetToBase);

public sealed class AffineConversionRule : IConversionRule
{
    private readonly IReadOnlyDictionary<Unit, UnitConversion> _map;

    public AffineConversionRule(IReadOnlyDictionary<Unit, UnitConversion> map) => _map = map;

    public bool CanConvert(Unit from, Unit to) =>
        from.Category == to.Category && _map.ContainsKey(from) && _map.ContainsKey(to);

    public decimal Convert(decimal value, Unit from, Unit to)
    {
        if (!CanConvert(from, to))
            throw new IncompatibleUnitsException(from, to);

        var f = _map[from];
        var t = _map[to];

        // value → base → target
        var inBase = value * f.ScaleToBase + f.OffsetToBase;
        return (inBase - t.OffsetToBase) / t.ScaleToBase;
    }
}
```

### 1.2 Worked examples (these become tests)

- **Length** (base = metre): `km` → `ScaleToBase = 1000, Offset = 0`; `m` → `(1, 0)`.
  `1000 m → km`: `inBase = 1000; result = 1000/1000 = 1`. ✅
- **Temperature** (base = kelvin):
  - `celsius` → `Scale = 1, Offset = 273.15`
  - `fahrenheit` → `Scale = 5/9, Offset = 255.372…`  (since `K = (F − 32)·5/9 + 273.15`)
  - `kelvin` → `(1, 0)`
  - `0 °C → °F`: `inBase = 0·1 + 273.15 = 273.15 K`; `F = (273.15 − 255.372…)/(5/9) = 32`. ✅

> Precision: use `decimal` throughout. For temperature, the Fahrenheit offset is
> irrational-ish in decimal; store derived constants at high precision or compute via the
> canonical formulae in a `FahrenheitConversion` helper to avoid drift. Round-trip tests
> assert `from→to→from` returns the original within a tolerance.

### 1.3 Domain exceptions

```csharp
public abstract class DomainException : Exception { protected DomainException(string m) : base(m) {} }

public sealed class UnknownUnitException(string symbol)
    : DomainException($"Unknown unit '{symbol}'.") { public string Symbol => symbol; }

public sealed class IncompatibleUnitsException(Unit from, Unit to)
    : DomainException($"Cannot convert '{from.Symbol}' ({from.Category.Name}) to "
                      + $"'{to.Symbol}' ({to.Category.Name}).");
```

## 2. Application layer (ports & use cases)

```csharp
namespace UnitConverter.Application;

// Port implemented by Infrastructure. Application depends on the abstraction, not the source.
public interface IUnitCatalog
{
    bool TryGetUnit(string symbol, out Unit unit);
    IReadOnlyCollection<UnitCategory> GetCategories();
    IReadOnlyCollection<Unit> GetUnits(string? categoryName = null);
    IConversionRule GetRuleFor(UnitCategory category);
}

public sealed record ConvertCommand(decimal Value, string FromUnit, string ToUnit);

public interface IConvertQuantityUseCase
{
    ConversionResult Handle(ConvertCommand command);
}

public sealed class ConvertQuantityUseCase(IUnitCatalog catalog) : IConvertQuantityUseCase
{
    public ConversionResult Handle(ConvertCommand command)
    {
        if (!catalog.TryGetUnit(command.FromUnit, out var from))
            throw new UnknownUnitException(command.FromUnit);
        if (!catalog.TryGetUnit(command.ToUnit, out var to))
            throw new UnknownUnitException(command.ToUnit);

        var rule = catalog.GetRuleFor(from.Category);   // resolved by category
        var value = rule.Convert(command.Value, from, to);
        return new ConversionResult(value, to);
    }
}
```

**Why a port (`IUnitCatalog`)?** It lets unit tests inject a fake/mock catalog (Moq) and
lets the catalog source evolve (code → JSON → DB) without touching use cases.

## 3. Infrastructure — unit catalog

v1 ships an **in-memory / config-backed** catalog. Registration is data-driven so adding a
category is additive (Open/Closed).

```csharp
public sealed class InMemoryUnitCatalog : IUnitCatalog
{
    // built from a static seed or appsettings/JSON; one AffineConversionRule per category
    // (each category's rule holds the Unit → UnitConversion map for that category)
}
```

Seed example (illustrative):

| Category | Unit symbol | Name | ScaleToBase | OffsetToBase |
|----------|-------------|------|-------------|--------------|
| length | m | metre | 1 | 0 |
| length | km | kilometre | 1000 | 0 |
| length | cm | centimetre | 0.01 | 0 |
| weight | kg | kilogram | 1 | 0 |
| weight | g | gram | 0.001 | 0 |
| weight | lb | pound | 0.45359237 | 0 |
| temperature | kelvin | kelvin | 1 | 0 |
| temperature | celsius | Celsius | 1 | 273.15 |
| temperature | fahrenheit | Fahrenheit | 0.555… (5/9) | 255.372… |

## 4. API layer (thin adapters)

DTOs are separate from domain records (don't leak the domain over the wire).

```csharp
public sealed record ConversionRequest(decimal Value, string FromUnit, string ToUnit);
public sealed record ConversionResponse(decimal Value, string FromUnit, string ToUnit, decimal ConvertedValue);
```

Controller (or minimal endpoint) — bind, delegate, map:

```csharp
[ApiController]
[Route("api/conversions")]
public sealed class ConversionsController(IConvertQuantityUseCase useCase) : ControllerBase
{
    [HttpPost]
    public ActionResult<ConversionResponse> Convert(ConversionRequest request)
    {
        var result = useCase.Handle(new ConvertCommand(request.Value, request.FromUnit, request.ToUnit));
        return Ok(new ConversionResponse(request.Value, request.FromUnit, request.ToUnit, result.Value));
    }
}
```

Domain exceptions are mapped centrally to `ProblemDetails` (see §8), so the controller stays
free of error-handling noise.

## 5. API contract (examples)

`POST /api/conversions`

```json
// request
{ "value": 1000, "fromUnit": "m", "toUnit": "km" }
```
```json
// 200 OK
{ "value": 1000, "fromUnit": "m", "toUnit": "km", "convertedValue": 1 }
```
```json
// 422 Unprocessable Entity (incompatible units) — RFC 7807
{
  "type": "https://httpstatuses.com/422",
  "title": "Incompatible units",
  "status": 422,
  "detail": "Cannot convert 'm' (length) to 'kg' (weight).",
  "traceId": "00-..."
}
```

`GET /api/units?category=length` → `200`

```json
[
  { "symbol": "m",  "name": "metre",      "category": "length" },
  { "symbol": "km", "name": "kilometre",  "category": "length" }
]
```

## 6. Validation

- **Edge validation** (API): non-empty `fromUnit`/`toUnit`, `value` is a finite decimal.
- **Domain validation**: unknown unit → `404`; cross-category → `422`.
- Prefer a small validator (DataAnnotations or FluentValidation) at the boundary; keep
  invariants in the domain. Don't duplicate business rules in the controller.

## 7. Test design

| Test project | Framework | Scope | Notes |
|--------------|-----------|-------|-------|
| `*.Domain.Tests` | MSTest | Conversion math, invariants, exceptions | No mocks needed; pure functions. Round-trip + boundary cases. |
| `*.Application.Tests` | MSTest + **Moq** | Use cases, error paths | Mock `IUnitCatalog`; verify rule selection & exception mapping. |
| `*.Api.Tests` | MSTest + `WebApplicationFactory` | HTTP contract | Status codes, JSON shape, ProblemDetails. |
| `*.Bdd.Tests` | **Reqnroll** + MSTest | Business-readable behaviour | `.feature` files in Gherkin; steps drive the use case or the API. |

Example unit test (MSTest):

```csharp
[TestClass]
public sealed class LengthConversionTests
{
    [TestMethod]
    public void Convert_MetresToKilometres_DividesByThousand()
    {
        var rule = LengthRuleFactory.Create();       // arrange
        var result = rule.Convert(1000m, Units.Metre, Units.Kilometre); // act
        Assert.AreEqual(1m, result);                 // assert
    }
}
```

Example use-case test with Moq:

```csharp
[TestMethod]
public void Handle_UnknownFromUnit_Throws()
{
    var catalog = new Mock<IUnitCatalog>();
    Unit dummy;
    catalog.Setup(c => c.TryGetUnit("nope", out dummy)).Returns(false);

    var sut = new ConvertQuantityUseCase(catalog.Object);

    Assert.ThrowsException<UnknownUnitException>(
        () => sut.Handle(new ConvertCommand(1, "nope", "m")));
}
```

Example Reqnroll feature:

```gherkin
Feature: Temperature conversion

  Scenario: Convert Celsius to Fahrenheit
    Given the value is 0
    And the source unit is "celsius"
    And the target unit is "fahrenheit"
    When I convert the value
    Then the converted value should be 32
```

## 8. Cross-cutting & security baseline

- **Error mapping:** central exception handler / middleware maps domain exceptions →
  `ProblemDetails` (`UnknownUnit`→404, `IncompatibleUnits`→422, validation→400). Never leak
  stack traces; attach `traceId`/correlation id.
- **HTTPS** enforced; OpenAPI document published; Swagger UI in dev only.
- **Hooks ready** (enable when features need them): policy-based **authorization**,
  **rate limiting** (`AddRateLimiter`), request size limits, CORS for browser clients.
- **Input safety:** reject unknown units explicitly; validate decimals; bounded payloads.
- See ADR-0003 for the security rationale (informed by *Beyond ASP.NET Core Security*).

## 9. Logging

Use `ILogger<T>` with structured properties and scopes; never concatenate. Use the
`LoggerMessage` source generator for hot-path logs (zero-alloc, strongly typed).

```csharp
internal static partial class ConversionLog
{
    [LoggerMessage(Level = LogLevel.Information,
        Message = "Converted {Value} {FromUnit} to {ConvertedValue} {ToUnit} ({Category})")]
    public static partial void Converted(ILogger logger,
        decimal value, string fromUnit, decimal convertedValue, string toUnit, string category);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unknown unit '{Symbol}' requested")]
    public static partial void UnknownUnit(ILogger logger, string symbol);
}
```

- A **correlation id** (request `TraceId`) is attached to every log via a logging scope /
  middleware; the same id is returned to clients in `ProblemDetails.traceId`.
- **No PII or secrets** in logs (this API has none, but the rule stands). Map levels:
  `Information` for successful conversions (sampled if noisy), `Warning` for 4xx-class domain
  errors, `Error` for unexpected failures only.

## 10. Observability / telemetry (OpenTelemetry)

The Domain stays clean; instrumentation lives in Application/Infrastructure/Api. Define a
single `ActivitySource` and `Meter` for the app.

```csharp
public static class Telemetry
{
    public const string ServiceName = "UnitConverter";
    public static readonly ActivitySource ActivitySource = new(ServiceName);
    public static readonly Meter Meter = new(ServiceName);

    public static readonly Counter<long> ConversionsCount =
        Meter.CreateCounter<long>("unitconverter.conversions.count");
    public static readonly Histogram<double> ConversionDuration =
        Meter.CreateHistogram<double>("unitconverter.conversion.duration", unit: "ms");
    public static readonly Counter<long> ConversionErrors =
        Meter.CreateCounter<long>("unitconverter.conversions.errors.count");
}
```

Wiring (centralized in `ServiceDefaults`, à la .NET Aspire):

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(Telemetry.ServiceName))
    .WithTracing(t => t
        .AddSource(Telemetry.ServiceName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())          // OTLP_ENDPOINT → Aspire dashboard
    .WithMetrics(m => m
        .AddMeter(Telemetry.ServiceName)
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

builder.Logging.AddOpenTelemetry(o => { o.IncludeScopes = true; o.AddOtlpExporter(); });
```

Usage in the use case (record a span + metrics; tag low-cardinality dimensions only):

```csharp
using var activity = Telemetry.ActivitySource.StartActivity("convert");
activity?.SetTag("unit.from", from.Symbol);
activity?.SetTag("unit.to", to.Symbol);
activity?.SetTag("unit.category", from.Category.Name);

var sw = Stopwatch.GetTimestamp();
var value = rule.Convert(command.Value, from, to);
Telemetry.ConversionDuration.Record(Stopwatch.GetElapsedTime(sw).TotalMilliseconds);
Telemetry.ConversionsCount.Add(1,
    new("category", from.Category.Name), new("from", from.Symbol), new("to", to.Symbol));
```

> **Cardinality caution:** unit symbols are bounded, so tagging `from`/`to` is acceptable.
> Never tag with the raw `value` or unbounded user input.

### Reviewing telemetry locally

- **Option A (recommended):** run `UnitConverter.AppHost` (Aspire) → it launches the API and
  opens the **Aspire dashboard** showing live traces, metrics, and logs.
- **Option B:** run the **standalone Aspire dashboard** container and set
  `OTEL_EXPORTER_OTLP_ENDPOINT` on the API, then `dotnet run` the API directly.
- **Option C (alternatives):** Seq (logs), Jaeger (traces), Prometheus + Grafana (metrics).

## 11. Caching

Cache **stable catalog reads**, not the arithmetic.

```csharp
// GET /api/units & /api/categories — read-through cache with explicit TTL.
// Prefer HybridCache (.NET 9+); IMemoryCache is the fallback.
var units = await cache.GetOrCreateAsync(
    $"units:{category ?? "all"}",
    async ct => catalog.GetUnits(category),
    new HybridCacheEntryOptions { Expiration = TimeSpan.FromHours(1) },
    cancellationToken: ct);
```

- **Invalidation:** v1 catalog is static, so TTL is enough; if the catalog becomes editable,
  evict by tag/key on change.
- **Scaling across instances:** behind a load balancer the L1 (in-process) cache is correct
  because the catalog is **immutable and identical** on every node. If the catalog becomes
  dynamic, `HybridCache` adds an **L2 distributed backend (Redis)** via
  `AddStackExchangeRedisCache` + `AddHybridCache` — a configuration change, not a redesign
  (see ADR-0005).
- **HTTP layer:** add **output caching** for `GET` catalog endpoints (`AddOutputCache`) and
  appropriate `Cache-Control`/`ETag` headers for browser/CDN reuse.
- **Not cached:** `POST /api/conversions` results — cheap pure math; caching adds staleness +
  memory for no measurable gain (revisit only if a profiler proves otherwise).

## 12. Performance tests

Two tiers, both out of the PR gate (run nightly / on demand):

**Micro-benchmarks — BenchmarkDotNet** (`perf/UnitConverter.Benchmarks`):

```csharp
[MemoryDiagnoser]
public class ConversionBenchmarks
{
    private readonly IConversionRule _length = LengthRuleFactory.Create();

    [Benchmark]
    public decimal MetresToKilometres() => _length.Convert(1000m, Units.Metre, Units.Kilometre);

    [Benchmark]
    public decimal CelsiusToFahrenheit() => _temp.Convert(0m, Units.Celsius, Units.Fahrenheit);
}
```

Measures time + allocations on the engine hot path; commit baseline results to catch regressions.

**Load / throughput — NBomber** (`tests/UnitConverter.Load.Tests`) against a running instance:

```csharp
var scenario = Scenario.Create("convert", async ctx =>
{
    var resp = await httpClient.PostAsJsonAsync("/api/conversions",
        new { value = 1000, fromUnit = "m", toUnit = "km" });
    return resp.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
})
.WithLoadSimulations(Simulation.Inject(rate: 200, interval: TimeSpan.FromSeconds(1),
                                       during: TimeSpan.FromMinutes(1)));
```

Assert on **p95/p99 latency** and **error rate** thresholds; NBomber emits HTML/CSV reports
you can review locally (k6 is an equivalent non-.NET option).

**Load vs stress vs spike vs soak** — all expressed as NBomber load simulations:

| Tier | Goal | Simulation shape |
|------|------|------------------|
| **Load** | Hold SLOs at expected concurrency | `Inject` at a steady target rate |
| **Stress** | Find the breaking point | Ramp the rate upward until errors/latency degrade |
| **Spike** | Survive sudden surges | Jump low → very high rate instantly, then recover |
| **Soak/endurance** | No leaks/degradation over time | Moderate rate held for hours; watch memory/GC + `unitconverter.*` metrics |

```csharp
.WithLoadSimulations(
    Simulation.RampingInject(rate: 1000, interval: TimeSpan.FromSeconds(1),    // stress ramp
                             during: TimeSpan.FromMinutes(5)),
    Simulation.Inject(rate: 5000, interval: TimeSpan.FromSeconds(1),           // spike
                      during: TimeSpan.FromSeconds(30)))
```

> **Generator placement:** at high RPS, run the load generator on a **separate host** from
> the API, or you measure the generator's ceiling, not the service. During runs, watch the
> Aspire dashboard (latency histogram, error counter, GC) to spot the true bottleneck.

## 13. Extensibility checklist — "add a new unit/category"

1. Add the unit row(s) with `ScaleToBase`/`OffsetToBase` to the catalog seed/config.
2. (New category only) ensure a rule is registered for it (the generic `AffineConversionRule` usually suffices).
3. Add domain tests for the new math (TDD) and a BDD scenario.
4. **No controller or endpoint change required.**
