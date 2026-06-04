# Implementation Checklist: High-Priority Fixes

**Start here** to implement the most impactful improvements. Each task includes files to modify and code samples.

---

## 1️⃣ Consolidate Exception Handling (1-2 hours) ⭐ HIGH IMPACT

**Why:** Two middleware handlers duplicate error-mapping logic; consolidating reduces maintenance burden.

**Files to modify:**
- `src/UnitConverter.UnitsDefinitions.Api/Program.cs`
- `src/UnitConverter.UnitsDefinitions.Api/Middleware/GlobalExceptionHandlingMiddleware.cs` (NEW)
- `src/UnitConverter.UnitsDefinitions.Api/Middleware/ConversionExceptionHandlingMiddleware.cs` (DELETE)
- `src/UnitConverter.UnitsDefinitions.Api/Middleware/UnitCatalogExceptionHandlingMiddleware.cs` (DELETE)

**Steps:**
1. Create `GlobalExceptionHandlingMiddleware` that handles ALL exception types
2. Map exceptions to ProblemDetails (conversion, catalog, validation, generic)
3. Remove two old middleware handlers
4. Update `Program.cs` to use single middleware
5. Test all exception paths

**Code Skeleton:**
```csharp
// src/UnitConverter.UnitsDefinitions.Api/Middleware/GlobalExceptionHandlingMiddleware.cs
public sealed class GlobalExceptionHandlingMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

	public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
	{
		_next = next;
		_logger = logger;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		try
		{
			await _next(context);
		}
		catch (Exception ex)
		{
			await HandleExceptionAsync(context, ex);
		}
	}

	private static Task HandleExceptionAsync(HttpContext context, Exception exception)
	{
		var (statusCode, problemDetails) = exception switch
		{
			UnitDuplicateException dup => (StatusCodes.Status409Conflict, new ProblemDetails
			{
				Type = "https://unitconverter.local/errors/unit-duplicate",
				Title = "Unit already exists",
				Detail = dup.Message,
				Status = StatusCodes.Status409Conflict,
				TraceId = context.TraceIdentifier
			}),
			UnitNotFoundException notFound => (StatusCodes.Status404NotFound, new ProblemDetails
			{
				Type = "https://unitconverter.local/errors/unit-not-found",
				Title = "Unit not found",
				Detail = notFound.Message,
				Status = StatusCodes.Status404NotFound,
				TraceId = context.TraceIdentifier
			}),
			ValidationException val => (StatusCodes.Status422UnprocessableEntity, new ProblemDetails
			{
				Type = "https://unitconverter.local/errors/validation-failed",
				Title = "Validation failed",
				Detail = string.Join("; ", val.Errors.Select(e => e.ErrorMessage)),
				Status = StatusCodes.Status422UnprocessableEntity,
				TraceId = context.TraceIdentifier,
				Extensions = new Dictionary<string, object?>
				{
					{ "errors", val.Errors.GroupBy(e => e.PropertyName)
						.ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()) }
				}
			}),
			_ => (StatusCodes.Status500InternalServerError, new ProblemDetails
			{
				Type = "https://unitconverter.local/errors/internal-error",
				Title = "Internal server error",
				Detail = "An unexpected error occurred",
				Status = StatusCodes.Status500InternalServerError,
				TraceId = context.TraceIdentifier
			})
		};

		context.Response.ContentType = "application/problem+json";
		context.Response.StatusCode = statusCode;
		return context.Response.WriteAsJsonAsync(problemDetails);
	}
}
```

**In Program.cs:**
```csharp
// OLD (remove these lines):
app.UseMiddleware<ConversionExceptionHandlingMiddleware>();
app.UseMiddleware<UnitCatalogExceptionHandlingMiddleware>();

// NEW (add this line):
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
```

**Tests to add:**
- Exception → ProblemDetails mapping for each exception type
- HTTP status codes (409, 404, 422, 500)
- TraceId presence

---

## 2️⃣ Move Pagination to SQL (1 hour) ⭐ QUICK WIN

**Why:** Current in-memory pagination loads all data from DB; should use SQL OFFSET/FETCH NEXT.

**Files to modify:**
- `src/UnitConverter.UnitsDefinitions.DataAccess/Persistence/UnitCatalogAdminRepository.cs`

**Current Code (Slow):**
```csharp
public async Task<PagedResult<CatalogUnit>> ListEntriesPagedAsync(
	UnitCategory? category, int page, int pageSize, CancellationToken cancellationToken)
{
	var query = _context.Units.AsQueryable();
	if (category.HasValue)
		query = query.Where(u => u.Category == category);

	var total = await query.CountAsync(cancellationToken);
	var items = await query
		.Skip((page - 1) * pageSize)  // ← LOADS ALL THEN SKIPS (WRONG!)
		.Take(pageSize)
		.ToListAsync(cancellationToken);

	return new PagedResult<CatalogUnit>(items, total, page, pageSize);
}
```

**Fixed Code (Fast):**
```csharp
public async Task<PagedResult<CatalogUnit>> ListEntriesPagedAsync(
	UnitCategory? category, int page, int pageSize, CancellationToken cancellationToken)
{
	var query = _context.Units.AsQueryable();
	if (category.HasValue)
		query = query.Where(u => u.Category == category);

	var total = await query.CountAsync(cancellationToken);

	var items = await query
		.OrderBy(u => u.Id)  // Add sorting for deterministic paging
		.Skip((page - 1) * pageSize)  // ← NOW SQL-level (OFFSET)
		.Take(pageSize)               // ← NOW SQL-level (FETCH NEXT)
		.AsNoTracking()               // Optional: perf boost for read-only
		.ToListAsync(cancellationToken);

	return new PagedResult<CatalogUnit>(items, total, page, pageSize);
}
```

**Key Changes:**
- Add `OrderBy()` before Skip/Take (SQL requires deterministic order)
- Add `.AsNoTracking()` (no change tracking needed for read-only queries)

**Tests to add:**
- Verify correct items returned for page 1, 2, 3
- Verify total count is accurate

---

## 3️⃣ Complete Input Validators (1-2 hours) ⭐ HIGH IMPACT

**Why:** Some validators missing rules; incomplete validation lets bad data through.

**Files to modify:**
- `src/UnitConverter.UnitsDefinitions.Api/Validators/CreateUnitRequestValidator.cs`
- `src/UnitConverter.UnitsDefinitions.Api/Validators/UpdateUnitRequestValidator.cs`
- `src/UnitConverter.UnitsDefinitions.Api/Validators/RejectUnitRequestValidator.cs`

**Example Fix (RejectUnitRequestValidator):**
```csharp
// CURRENT (incomplete):
public class RejectUnitRequestValidator : AbstractValidator<RejectUnitRequest>
{
	public RejectUnitRequestValidator()
	{
		// Missing: Reason validation!
	}
}

// FIXED (complete):
public class RejectUnitRequestValidator : AbstractValidator<RejectUnitRequest>
{
	public RejectUnitRequestValidator()
	{
		RuleFor(x => x.Reason)
			.NotNull()
			.WithMessage("Reason is required")
			.NotEmpty()
			.WithMessage("Reason cannot be empty")
			.MaximumLength(500)
			.WithMessage("Reason cannot exceed 500 characters");
	}
}
```

**Audit Checklist:**
- [ ] CreateUnitRequestValidator: Symbol not null/empty, DisplayName rules, Category valid, Multiplier/Offset valid decimals
- [ ] UpdateUnitRequestValidator: Same as Create (minus symbol uniqueness)
- [ ] RejectUnitRequestValidator: Reason not null, max length, trim

**Tests to add:**
- Valid requests pass
- Null/empty fields rejected
- Length limits enforced
- Invalid category rejected

---

## 4️⃣ Add OpenTelemetry Instrumentation (3-4 hours) ⭐ ADR-0004

**Why:** ADR-0004 specifies OpenTelemetry; needed for observability dashboard (Aspire).

**Files to modify:**
- `src/UnitConverter.UnitsDefinitions.Api/Program.cs`
- `src/UnitConverter.UnitsDefinitions.Api/Services/ConvertUnitsHandler.cs` (add tracing)
- `src/UnitConverter.Common/DependencyInjection/ObservabilityServiceCollectionExtensions.cs` (NEW)

**Steps:**
1. Install NuGet packages:
   - `OpenTelemetry`
   - `OpenTelemetry.Extensions.Hosting`
   - `OpenTelemetry.Instrumentation.AspNetCore`
   - `OpenTelemetry.Instrumentation.Http`
   - `OpenTelemetry.Exporter.OpenTelemetryProtocol`

2. Add OpenTelemetry setup in Program.cs:
```csharp
builder.Services
	.AddOpenTelemetry()
	.WithMetrics(meter => meter
		.AddAspNetCoreInstrumentation()
		.AddHttpClientInstrumentation()
		.AddRuntimeInstrumentation())
	.WithTracing(tracer => tracer
		.AddAspNetCoreInstrumentation()
		.AddHttpClientInstrumentation()
		.AddSource("UnitConverter"))
	.WithLogging(logging => logging
		.AddOtlpExporter());
```

3. Instrument conversion handler:
```csharp
private static readonly ActivitySource _activitySource = new("UnitConverter");

public async Task<ConversionResult> HandleAsync(ConvertUnitRequest request, CancellationToken cancellationToken)
{
	using var activity = _activitySource.StartActivity("ConvertUnits", ActivityKind.Internal);
	activity?.SetTag("unit.from", request.FromUnit);
	activity?.SetTag("unit.to", request.ToUnit);
	activity?.SetTag("value", request.Value);

	// ... conversion logic ...
}
```

4. Set OTEL_EXPORTER_OTLP_ENDPOINT to local dashboard (Docker or standalone)

---

## 5️⃣ Add Caching to Catalog Reads (1-2 hours) ⭐ PERFORMANCE

**Why:** Catalog data is stable; caching reduces DB queries by 90%+.

**Files to modify:**
- `src/UnitConverter.UnitsDefinitions.Api/Services/CatalogQueries.cs`
- `src/UnitConverter.UnitsDefinitions.Api/Program.cs`

**Steps:**
1. Add HybridCache to DI:
```csharp
builder.Services.AddHybridCache(options =>
{
	options.DefaultExpiration = TimeSpan.FromMinutes(10);
});
```

2. Inject cache in CatalogQueries:
```csharp
private readonly IHybridCache _cache;

public CatalogQueries(IHybridCache cache, ...)
{
	_cache = cache;
}

public async Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken)
{
	return await _cache.GetOrCreateAsync(
		"catalog:categories",
		async token => await _context.Categories.ToListAsync(token),
		cancellationToken: cancellationToken);
}
```

3. Invalidate cache on writes:
```csharp
public async Task AddUnitAsync(UnitDefinition unit, ...)
{
	await _repository.AddAsync(unit, ...);
	await _cache.RemoveAsync("catalog:categories", cancellationToken);
	await _cache.RemoveAsync("catalog:units", cancellationToken);
}
```

**Tests to add:**
- Cache miss on first call
- Cache hit on second call
- Cache invalidation on write

---

## 📅 Implementation Order

**Week 1:**
1. ✅ Consolidate exception handling (high impact)
2. ✅ Move pagination to SQL (quick win)
3. ✅ Complete validators (robustness)

**Week 2:**
4. 🔲 Add OpenTelemetry (ADR compliance)
5. 🔲 Add caching (performance)

**Week 3:**
6. 🔲 Add structured logging (observability)
7. 🔲 Add load tests (performance validation)

---

## 🧪 Verification

After each fix, verify with:
```powershell
# Build
dotnet build

# Run tests
dotnet test

# Run API (check for errors)
dotnet run --project src/UnitConverter.UnitsDefinitions.Api
```

---

**Ready to start?** Pick the highest priority item above and follow the code skeleton provided. Each task includes concrete before/after code samples.
