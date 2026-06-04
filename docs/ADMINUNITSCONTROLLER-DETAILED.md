# AdminUnitsController Deep Dive: Line-by-Line Comparison

**File:** `src/UnitConverter.UnitsDefinitions.Api/Controllers/AdminUnitsController.cs`

---

## 📋 Quick Reference: Issues by Line

| Lines | Issue | Severity | Fix |
|-------|-------|----------|-----|
| 30–33 | No null check in constructor | 🟡 Low | Add `ArgumentNullException.ThrowIfNull(admin)` |
| 35–45 | Missing validation on ListAsync parameters | 🟠 Medium | Add [Range] attributes, ProducesResponseType 400 |
| 47–54 | Missing validation on id parameter | 🟡 Low | Add constraint check |
| 64–80 | **Duplicated exception handling (Create)** | 🔴 **CRITICAL** | Implement global exception handler |
| 72 | **String-based error mapping** | 🔴 **CRITICAL** | Use custom exception types |
| 82–103 | **Duplicated exception handling (Update)** | 🔴 **CRITICAL** | Remove try/catch (use middleware) |
| 105–124 | **Duplicated exception handling (Approve)** | 🔴 **CRITICAL** | Remove try/catch (use middleware) |
| 126–148 | **Duplicated exception handling (Reject)** | 🔴 **CRITICAL** | Remove try/catch (use middleware) |
| 150–162 | OverrideAsync — **GOOD example** | ✅ | Follow this pattern (delegate, no duplication) |
| 164–180 | DeleteAsync — Inconsistent handling | 🟠 Medium | Make consistent with middleware pattern |

---

## 🔴 CRITICAL ISSUES DETAILED

### Issue #1: Exception Handling Duplication

#### Locations
- **CreateAsync:** Lines 64–80 (17 lines)
- **UpdateAsync:** Lines 91–103 (13 lines)
- **ApproveAsync:** Lines 112–124 (13 lines)
- **RejectAsync:** Lines 136–148 (13 lines)
- **Total Duplication:** ~56 lines of identical logic

#### Current Pattern (Repeated 4 Times)

```csharp
// Pattern A (CreateAsync - Lines 64-80)
try
{
	var created = await _admin.CreateAsync(request, cancellationToken);
	return CreatedAtAction(..., created);
}
catch (UnitCatalogException ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
{
	return Conflict(new { error = ex.Message });
}
catch (UnitCatalogException ex)
{
	return BadRequest(new { error = ex.Message });
}

// Pattern B (UpdateAsync/ApproveAsync/RejectAsync - Lines 91-103, 112-124, 136-148)
try
{
	return Ok(await _admin.UpdateAsync(...));
}
catch (UnitCatalogException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
{
	return NotFound(new { error = ex.Message });
}
catch (UnitCatalogException ex)
{
	return BadRequest(new { error = ex.Message });
}

// Pattern C (DeleteAsync - Lines 171-179)
try
{
	await _admin.DeleteAsync(id, cancellationToken);
	return NoContent();
}
catch (UnitCatalogException ex)
{
	return NotFound(new { error = ex.Message });
}
```

#### Problems with Current Approach

| Problem | Impact | Example |
|---------|--------|---------|
| **String parsing in when clause** | Brittle; breaks if message changes | If message: "already exists" → "duplicate", code breaks |
| **Multiple catch blocks per method** | Duplicated logic | Same 4 lines repeated 4 times |
| **No semantic intent** | Hard to test | Exception type doesn't convey meaning; message does |
| **Maintenance burden** | Every change must be replicated | Add new exception type? Update 4 places |
| **Mixed responsibility** | Controller handles HTTP + exception mapping | Should delegate to middleware |

---

### Issue #2: String-Based Error Mapping

#### Line 72 Example
```csharp
catch (UnitCatalogException ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
{
	return Conflict(new { error = ex.Message });
}
```

#### Why This Is Brittle

**Scenario 1: Message Change**
```csharp
// If UnitCatalogService changes message:
throw new UnitCatalogException("Unit symbol is a duplicate"); // Used to say "already exists"
// Controller.CreateAsync still returns 400, not 409 (silent bug!)
```

**Scenario 2: Multiple Similar Messages**
```csharp
// What if service throws:
throw new UnitCatalogException("Symbol 'm' already exists in Length category");
throw new UnitCatalogException("Could not fetch unit (already deleted)");
// Both match "already exists"; second gets 409 instead of 500 (wrong!)
```

**Scenario 3: Internationalization**
```csharp
// In Portuguese: "já existe"
// In Spanish: "ya existe"
// String comparison breaks in different cultures
```

---

## ✅ RECOMMENDED SOLUTION: Global Exception Handler

### Step 1: Define Custom Exception Types

```csharp
// File: src/UnitConverter.UnitsDefinitions/Catalog/UnitDuplicateException.cs
namespace UnitConverter.UnitsDefinitions.Catalog;

public sealed class UnitDuplicateException : UnitCatalogException
{
	public string Symbol { get; }
	public UnitCategory Category { get; }

	public UnitDuplicateException(string symbol, UnitCategory category)
		: base($"Unit '{symbol}' already exists in category {category}.")
	{
		Symbol = symbol;
		Category = category;
	}
}

// File: src/UnitConverter.UnitsDefinitions/Catalog/UnitNotFoundException.cs
namespace UnitConverter.UnitsDefinitions.Catalog;

public sealed class UnitNotFoundException : UnitCatalogException
{
	public int UnitId { get; }

	public UnitNotFoundException(int id)
		: base($"Unit with id {id} was not found.")
	{
		UnitId = id;
	}
}
```

### Step 2: Update Service to Throw Specific Exceptions

```csharp
// File: src/UnitConverter.UnitsDefinitions.Api/Services/UnitAdminService.cs
public async Task<UnitDetailResponse> CreateAsync(
	CreateUnitRequest request,
	CancellationToken cancellationToken = default)
{
	ArgumentNullException.ThrowIfNull(request);
	ValidateMultiplier(request.MultiplierToBase);

	// ✅ CHANGED: Throw specific exception type (not generic + string)
	if (await _repository.ExistsBySymbolAsync(request.Symbol, request.Category, cancellationToken: cancellationToken))
	{
		throw new UnitDuplicateException(request.Symbol, request.Category);
	}

	var unit = new UnitDefinition(...);
	var initialStatus = _currentUser.IsAdmin ? UnitApprovalStatus.Approved : UnitApprovalStatus.PendingApproval;
	var id = await _repository.AddAsync(unit, _currentUser.Email, initialStatus, cancellationToken);
	var created = await _repository.GetEntryByIdAsync(id, cancellationToken)
		?? throw new UnitCatalogException("Unit was created but could not be loaded.");

	return ToResponse(created);
}

public async Task<UnitDetailResponse> UpdateAsync(
	int id,
	UpdateUnitRequest request,
	CancellationToken cancellationToken = default)
{
	ArgumentNullException.ThrowIfNull(request);

	// ✅ CHANGED: Throw specific exception type
	if (await _repository.GetEntryByIdAsync(id, cancellationToken) is null)
	{
		throw new UnitNotFoundException(id);
	}

	// ... rest of method
}
```

### Step 3: Create Global Exception Handler Middleware

```csharp
// File: src/UnitConverter.Common/Middleware/GlobalExceptionHandlingMiddleware.cs
using System.Text.Json;
using UnitConverter.Common.Constants;
using UnitConverter.Common.Contracts.Responses;
using UnitConverter.UnitsDefinitions.Catalog;

namespace UnitConverter.Common.Middleware;

public sealed class GlobalExceptionHandlingMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

	public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
	{
		_next = next ?? throw new ArgumentNullException(nameof(next));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task InvokeAsync(HttpContext context)
	{
		try
		{
			await _next(context);
		}
		catch (Exception ex)
		{
			await HandleExceptionAsync(context, ex, _logger);
		}
	}

	private static Task HandleExceptionAsync(
		HttpContext context,
		Exception ex,
		ILogger<GlobalExceptionHandlingMiddleware> logger)
	{
		// Map exception type to HTTP status code
		var (statusCode, title) = ex switch
		{
			UnitDuplicateException => 
				(StatusCodes.Status409Conflict, "Conflict"),

			UnitNotFoundException => 
				(StatusCodes.Status404NotFound, "Not Found"),

			UnitCatalogException => 
				(StatusCodes.Status400BadRequest, "Bad Request"),

			ConversionException => 
				(StatusCodes.Status400BadRequest, "Bad Request"),

			ArgumentNullException or ArgumentException => 
				(StatusCodes.Status400BadRequest, "Bad Request"),

			_ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
		};

		logger.LogWarning(
			ex,
			"Unhandled exception of type {ExceptionType}: {Message}",
			ex.GetType().Name,
			ex.Message);

		var error = new ErrorResponse(
			Type: ApiErrorTypes.ForStatusCode(statusCode),
			Title: title,
			Status: statusCode,
			Detail: ex.Message,
			TraceId: context.TraceIdentifier,
			CorrelationId: context.TraceIdentifier);

		context.Response.StatusCode = statusCode;
		context.Response.ContentType = MediaTypes.ProblemJson;

		var json = JsonSerializer.Serialize(error, new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		});

		return context.Response.WriteAsync(json);
	}
}
```

### Step 4: Simplify Controller Methods

```csharp
// File: src/UnitConverter.UnitsDefinitions.Api/Controllers/AdminUnitsController.cs

[HttpPost]
[ProducesResponseType(typeof(UnitDetailResponse), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status409Conflict)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<ActionResult<UnitDetailResponse>> CreateAsync(
	[FromBody] CreateUnitRequest request,
	CancellationToken cancellationToken)
{
	// ✅ CHANGED: No try/catch; middleware handles exceptions
	var created = await _admin.CreateAsync(request, cancellationToken);
	return CreatedAtAction(
		nameof(GetByIdAsync),
		new { id = created.Id, version = AppApiVersions.V1UrlSegment },
		created);
}

[HttpPut("{id:int}")]
[ProducesResponseType(typeof(UnitDetailResponse), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<ActionResult<UnitDetailResponse>> UpdateAsync(
	int id,
	[FromBody] UpdateUnitRequest request,
	CancellationToken cancellationToken)
{
	// ✅ CHANGED: No try/catch; much cleaner
	return Ok(await _admin.UpdateAsync(id, request, cancellationToken));
}

[HttpPut("{id:int}/approve")]
[Authorize(Policy = AuthorizationPolicies.MasterDataAdminApprove)]
[ProducesResponseType(typeof(UnitDetailResponse), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<ActionResult<UnitDetailResponse>> ApproveAsync(int id, CancellationToken cancellationToken)
{
	// ✅ CHANGED: Simplified
	return Ok(await _admin.ApproveAsync(id, cancellationToken));
}

[HttpPut("{id:int}/reject")]
[Authorize(Policy = AuthorizationPolicies.MasterDataAdminApprove)]
[ProducesResponseType(typeof(UnitDetailResponse), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<ActionResult<UnitDetailResponse>> RejectAsync(
	int id,
	[FromBody] RejectUnitRequest request,
	CancellationToken cancellationToken)
{
	// ✅ CHANGED: Simplified
	return Ok(await _admin.RejectAsync(id, request, cancellationToken));
}

[HttpDelete("{id:int}")]
[Authorize(Policy = AuthorizationPolicies.MasterDataAdminDelete)]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken)
{
	// ✅ CHANGED: Simplified
	await _admin.DeleteAsync(id, cancellationToken);
	return NoContent();
}
```

### Step 5: Register Middleware in Program.cs

```csharp
// File: src/UnitConverter.UnitsDefinitions.Api/Program.cs
var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
	await app.EnsureEfMigrationsAppliedAsync<ConverterDbContext>();
}

// ✅ ADD THIS (before other middleware):
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseUnitConverterCors();
app.UseRateLimiter();
app.UseUnitConverterApiSecurity();
app.UseMiddleware<ConversionExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
```

---

## 📊 Before & After Comparison

### Line Count Comparison

```
BEFORE:
  CreateAsync:   17 lines (64-80)
  UpdateAsync:   13 lines (91-103)
  ApproveAsync:  13 lines (112-124)
  RejectAsync:   13 lines (136-148)
  DeleteAsync:    9 lines (171-179)
  ─────────────
  Total:         65 lines of exception handling

AFTER:
  CreateAsync:    5 lines (call service, return CreatedAtAction)
  UpdateAsync:    3 lines (call service, return Ok)
  ApproveAsync:   3 lines (call service, return Ok)
  RejectAsync:    3 lines (call service, return Ok)
  DeleteAsync:    3 lines (call service, return NoContent)
  ─────────────
  Total:         17 lines

  Middleware:    ~60 lines (defined once, reused everywhere)
  ─────────────
  NET REDUCTION: ~8 lines (20 vs 65), 70% less duplication
```

### Testability Comparison

```
BEFORE:
  To test error handling, must:
	1. Create mock IUnitAdminService that throws
	2. Call controller method
	3. Assert response status/body

  Problem: Test is fragile to exception message changes

AFTER:
  To test error handling, can:
	1. Unit test middleware directly (inject exception)
	2. OR test service layer (which throws exceptions)
	3. OR test controller (which now has no error logic)

  Benefit: Exception mapping testable independently of controller
```

### Maintenance Comparison

```
BEFORE:
  "I need to map a new exception type to HTTP status"
  → Must update CreateAsync, UpdateAsync, ApproveAsync, RejectAsync, DeleteAsync (5 places)
  → Risk: Miss one and inconsistency appears

AFTER:
  "I need to map a new exception type"
  → Add one line to GlobalExceptionHandlingMiddleware switch statement
  → All controllers automatically updated
```

---

## 🎯 Impact Summary

| Metric | Before | After | Change |
|--------|--------|-------|--------|
| **Lines in controller** | 181 | 140 | -41 lines (-23%) |
| **Duplicated code** | 65 lines | 0 lines | -65 lines (100% elimination) |
| **Exception mapping locations** | 5 | 1 | -80% |
| **Testability** | Hard | Easy | ✅ |
| **Maintainability** | Brittle | Robust | ✅ |
| **Error consistency** | Inconsistent | Centralized | ✅ |

---

## Implementation Checklist

- [ ] Create `UnitDuplicateException` class
- [ ] Create `UnitNotFoundException` class
- [ ] Update `UnitAdminService` to throw specific exceptions
- [ ] Create `GlobalExceptionHandlingMiddleware`
- [ ] Register middleware in `Program.cs`
- [ ] Remove all try/catch blocks from `AdminUnitsController`
- [ ] Add missing `ProducesResponseType` for 401, 403
- [ ] Add null check to constructor
- [ ] Run tests: `dotnet test`
- [ ] Run API: `dotnet run --project src/UnitConverter.UnitsDefinitions.Api`
- [ ] Test endpoints manually (verify 409 on duplicate, 404 on not found, etc.)

---

## References

- [Global Exception Handling Pattern in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling)
- [Middleware in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware)
- [Problem Details for HTTP APIs (RFC 7807)](https://www.rfc-editor.org/rfc/rfc7807)
- [DRY Principle (Don't Repeat Yourself)](https://en.wikipedia.org/wiki/Don%27t_repeat_yourself)

---

**Grade After Fix: A** (from B+)

**Time to Implement: 4 hours**

**Impact: High** (eliminates main code smell, improves maintainability, testability, consistency)
