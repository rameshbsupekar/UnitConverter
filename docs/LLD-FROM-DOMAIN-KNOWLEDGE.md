# Domain Knowledge & LLD Summary

**Status:** Design complete, ready for implementation

---

## Understanding the Domain (Physics → Code)

### What is a Unit?

A **unit** is a standardized quantity for measurement. Each unit has:
- **Symbol** — unique identifier (e.g., "m", "kg", "°C")
- **Name** — human-readable (e.g., "metre", "kilogram")
- **Category** — what it measures (Length, Weight, Temperature, etc.)
- **Conversion metadata** — how to convert to/from the base unit in that category

### Categories (v1 Minimum)

| Category | Base Unit | Example Units | Conversion Type |
|----------|-----------|---|---|
| **Length** | Metre (m) | km (1000×), cm (0.01×), inch (0.0254×), foot (0.3048×), mile (1609.344×) | **Linear** (multiply by scale) |
| **Weight/Mass** | Kilogram (kg) | g (0.001×), lb (0.45359237×), oz (0.0283495×), tonne (1000×) | **Linear** |
| **Temperature** | Kelvin (K) | Celsius (offset: +273.15), Fahrenheit (scale: 5/9, offset: +255.372) | **Affine** (scale + offset) |

### Unit Conversion Math

#### Type 1: Linear (Length, Weight, most units)

```
value_base = value_source * scale_source
value_target = value_base / scale_target

Example: 1000 m → km
  value_base = 1000 * 1 = 1000 m
  value_km = 1000 / 1000 = 1 km ✓
```

#### Type 2: Affine (Temperature)

```
value_base = value_source * scale + offset
value_target = (value_base - offset_target) / scale_target

Example: 0°C → °F
  value_base = 0 * 1 + 273.15 = 273.15 K
  value_F = (273.15 - 255.372) / (5/9) = 32 °F ✓
```

---

## Input/Output Specifications (API Contract)

### Input: Convert Request

```json
{
  "value": 1000,         // Decimal number (finite, can be negative)
  "fromUnit": "m",       // Unit symbol (lowercase, trimmed)
  "toUnit": "km"         // Unit symbol (lowercase, trimmed)
}
```

**Validation:**
- `value` must be finite (not NaN, not Infinity)
- `fromUnit` must be a known, approved unit symbol
- `toUnit` must be a known, approved unit symbol
- Both units must be in the **same category** (can't convert m to kg)

### Input: Submit Unit Request

```json
{
  "symbol": "foot",           // Unique in category, lowercase alphanumeric
  "name": "Foot",             // Human-readable, ≤ 100 chars
  "category": "length",       // Must exist (length, weight, temperature)
  "scaleToBase": 0.3048,      // Positive decimal, conversion factor
  "offsetToBase": 0           // Optional, for affine (default 0)
}
```

**Validation:**
- `symbol` must be unique in the category
- `symbol` must be 1–20 alphanumeric lowercase characters
- `name` must be non-empty, ≤ 100 characters
- `category` must exist in the system
- `scaleToBase` must be positive (> 0)
- `offsetToBase` must be finite (optional, default 0)

### Output: Conversion Response (200 OK)

```json
{
  "value": 1000,
  "fromUnit": "m",
  "toUnit": "km",
  "convertedValue": 1.0
}
```

**Properties:**
- `convertedValue` is a `decimal` with full precision
- Round-trip (A→B→A) maintains ±0.001% tolerance

### Output: Error Responses

**400 Bad Request** (validation failed)
```json
{
  "type": "https://example.com/validation-error",
  "title": "Validation Failed",
  "status": 400,
  "detail": "Field 'value' must be a valid decimal number.",
  "traceId": "00-abc123-def456-00"
}
```

**404 Not Found** (unknown unit)
```json
{
  "type": "https://example.com/unknown-unit",
  "title": "Unknown Unit",
  "status": 404,
  "detail": "Unit 'bazinga' not found.",
  "traceId": "00-abc123-def456-00"
}
```

**422 Unprocessable Entity** (incompatible units)
```json
{
  "type": "https://example.com/incompatible-units",
  "title": "Incompatible Units",
  "status": 422,
  "detail": "Cannot convert 'm' (length) to 'kg' (weight).",
  "traceId": "00-abc123-def456-00"
}
```

---

## Data Validation Rules (At Boundary)

### For Conversions

| Input | Rule | Invalid Example | HTTP Status |
|-------|------|---|---|
| `value` | Finite decimal | NaN, Infinity, "abc" | 400 |
| `fromUnit` | Known, approved symbol | "bazinga", "m" (pending) | 404 or 422 |
| `toUnit` | Known, approved symbol | "bazinga", "" | 404 or 422 |
| Compatibility | Same category | m to kg | 422 |

### For Unit Submission

| Input | Rule | Invalid Example | HTTP Status |
|-------|------|---|---|
| `symbol` | Unique, 1–20 chars, lowercase alphanumeric | "M", "", "m" (duplicate) | 400 or 409 |
| `name` | Non-empty, ≤ 100 chars | "", "x"*101 | 400 |
| `category` | Exists | "mass" (vs "weight") | 400 |
| `scaleToBase` | Positive | 0, -1, NaN | 400 |
| `offsetToBase` | Finite | NaN, Infinity | 400 |

---

## Low-Level Design (From Domain Knowledge)

### Domain Layer (Framework-free)

```csharp
namespace UnitConverter.Domain;

// Unit: immutable value object
public sealed record Unit(
    string Symbol,
    string Name,
    UnitCategory Category,
    UnitStatus Status,              // Pending, Approved, Rejected
    decimal ScaleToBase,            // Linear: 1, 1000, 0.0254; Affine: 1, 5/9
    decimal OffsetToBase = 0,       // Affine only: 0, 273.15, 255.372
    string? SubmittedBy = null,     // User ID
    DateTime? SubmittedAt = null,
    string? ApprovedBy = null,
    DateTime? ApprovedAt = null,
    string? RejectionReason = null
);

// Quantity: value + unit
public sealed record Quantity(decimal Value, Unit Unit);

// Conversion result
public sealed record ConversionResult(decimal Value, Unit Unit);

// Conversion rule: pluggable strategy
public interface IConversionRule
{
    /// <summary>
    /// Can this rule convert between two units?
    /// </summary>
    bool CanConvert(Unit from, Unit to);

    /// <summary>
    /// Apply the conversion.
    /// </summary>
    /// <exception cref="IncompatibleUnitsException">If units are incompatible.</exception>
    decimal Convert(decimal value, Unit from, Unit to);
}

// Affine conversion: handles linear + temperature
public sealed class AffineConversionRule : IConversionRule
{
    private readonly IReadOnlyDictionary<Unit, (decimal scale, decimal offset)> _metadata;

    public bool CanConvert(Unit from, Unit to) =>
        from.Category == to.Category && _metadata.ContainsKey(from) && _metadata.ContainsKey(to);

    public decimal Convert(decimal value, Unit from, Unit to)
    {
        if (!CanConvert(from, to))
            throw new IncompatibleUnitsException(from, to);

        var (fromScale, fromOffset) = _metadata[from];
        var (toScale, toOffset) = _metadata[to];

        // value → base unit → target unit
        var inBase = value * fromScale + fromOffset;
        return (inBase - toOffset) / toScale;
    }
}

// Domain exceptions
public abstract class DomainException : Exception
{
    protected DomainException(string msg) : base(msg) { }
}

public sealed class UnknownUnitException(string symbol)
    : DomainException($"Unknown unit: {symbol}");

public sealed class IncompatibleUnitsException(Unit from, Unit to)
    : DomainException($"Cannot convert '{from.Symbol}' ({from.Category.Name}) "
                    + $"to '{to.Symbol}' ({to.Category.Name}).");
```

### Application Layer (Use Cases)

```csharp
namespace UnitConverter.Application;

// CQRS: reads (queries)
public interface IUnitQueryRepository
{
    Task<Unit?> GetBySymbolAsync(string symbol, CancellationToken ct = default);
    Task<IReadOnlyList<Unit>> ListApprovedAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Unit>> ListApprovedByCategoryAsync(
        string categoryName, CancellationToken ct = default);
}

// CQRS: writes (commands)
public interface IUnitCommandRepository
{
    Task<Unit> CreateAsync(Unit unit, CancellationToken ct = default);
    Task UpdateAsync(Unit unit, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}

// Use case: convert
public sealed record ConvertCommand(decimal Value, string FromUnit, string ToUnit);

public interface IConvertQuantityUseCase
{
    /// <exception cref="UnknownUnitException"></exception>
    /// <exception cref="IncompatibleUnitsException"></exception>
    Task<ConversionResult> HandleAsync(ConvertCommand cmd, CancellationToken ct = default);
}

public sealed class ConvertQuantityUseCase(IUnitQueryRepository repo, IConversionRuleFactory factory)
    : IConvertQuantityUseCase
{
    public async Task<ConversionResult> HandleAsync(ConvertCommand cmd, CancellationToken ct)
    {
        var from = await repo.GetBySymbolAsync(cmd.FromUnit, ct);
        if (from is null)
            throw new UnknownUnitException(cmd.FromUnit);

        var to = await repo.GetBySymbolAsync(cmd.ToUnit, ct);
        if (to is null)
            throw new UnknownUnitException(cmd.ToUnit);

        var rule = factory.GetRuleFor(from.Category);
        var value = rule.Convert(cmd.Value, from, to);
        return new ConversionResult(value, to);
    }
}
```

### API Layer (Controllers, DTOs, Validation)

```csharp
namespace UnitConverter.Api;

[ApiController]
[Route("api/conversions")]
public sealed class ConversionsController(IConvertQuantityUseCase useCase) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ConversionResponse>> Convert(
        ConvertRequest request, CancellationToken ct)
    {
        // Validation (boundary)
        var validationResult = ValidateConvertRequest(request);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        // Call use case
        var result = await useCase.HandleAsync(
            new ConvertCommand(request.Value, request.FromUnit, request.ToUnit), ct);

        // Map to response
        return Ok(new ConversionResponse(
            request.Value, request.FromUnit, request.ToUnit, result.Value));
    }

    private ValidationResult ValidateConvertRequest(ConvertRequest req)
    {
        if (!decimal.TryParse(req.Value.ToString(), out var val) || decimal.IsNaN(val) || decimal.IsInfinity(val))
            return ValidationResult.Fail(nameof(req.Value), "Must be a finite decimal");
        
        if (string.IsNullOrWhiteSpace(req.FromUnit))
            return ValidationResult.Fail(nameof(req.FromUnit), "Required");
        
        if (string.IsNullOrWhiteSpace(req.ToUnit))
            return ValidationResult.Fail(nameof(req.ToUnit), "Required");

        return ValidationResult.Success();
    }
}

public sealed record ConvertRequest(decimal Value, string FromUnit, string ToUnit);
public sealed record ConversionResponse(
    decimal Value, string FromUnit, string ToUnit, decimal ConvertedValue);

// Error mapping middleware (centralized)
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var ex = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
    var traceId = context.TraceIdentifier;

    var statusCode = ex switch
    {
        UnknownUnitException => 404,
        IncompatibleUnitsException => 422,
        ValidationException => 400,
        _ => 500
    };

    var problem = new ProblemDetails
    {
        Status = statusCode,
        Title = ex?.GetType().Name,
        Detail = ex?.Message,
        Instance = context.Request.Path,
        Type = $"https://example.com/{ex?.GetType().Name.ToLower()}",
    };

    context.Response.StatusCode = statusCode;
    await context.Response.WriteAsJsonAsync(problem);
}));
```

---

## Precision & Rounding Strategy

### Problem
Temperature conversions can introduce floating-point errors on round-trip.

### Solution
1. **Use `decimal` type** (C# native, 128-bit, 28 significant digits)
2. **Accept ±0.001% tolerance** on round-trip tests
3. **Document it clearly** in API docs

### Test Example
```csharp
[TestMethod]
public void RoundTrip_Precision()
{
    var original = 98.6m;
    var celsius = Convert(original, "fahrenheit", "celsius");
    var back = Convert(celsius, "celsius", "fahrenheit");
    
    // Within 0.01 of original (0.01% tolerance)
    Assert.IsTrue(Math.Abs(original - back) < 0.01m);
}
```

---

## Ready to Implement

With this domain knowledge locked in, you can now:

1. **Milestone 0:** Scaffold solution structure
2. **Milestone 1:** Implement Domain layer (AffineConversionRule, domain exceptions)
3. **Milestone 2:** Implement Application layer (use cases)
4. **Milestone 3:** Wire BDD steps (Gherkin → domain)
5. **Milestone 4:** Build API (controllers, validation, error mapping)

All with TDD, starting from Gherkin requirements → domain logic → implementation.

