# Domain Interfaces Derived from BDD Features

**Approach:** Read each Gherkin feature, extract the domain operations, define clean interfaces.

---

## 1. Unit & Catalog Management

From: `Conversions.feature`, `UnitManagement.feature`, `ApprovalWorkflow.feature`

```csharp
namespace UnitConverter.Domain;

/// <summary>
/// Represents a unit of measurement (e.g., "meter", "kilogram", "celsius").
/// Immutable value object.
/// </summary>
public sealed record Unit(
    string Symbol,                    // e.g., "m", "km", "lb", "celsius"
    string Name,                      // e.g., "metre", "kilogram", "Celsius"
    UnitCategory Category,            // e.g., Length, Weight, Temperature
    UnitStatus Status,                // Pending, Approved, Rejected
    string SubmittedBy,               // User ID (employee/partner) who submitted
    DateTime SubmittedAt,             // When submitted
    string? ApprovedBy = null,        // Admin user ID (if approved)
    DateTime? ApprovedAt = null,      // When approved
    string? RejectionReason = null    // Why rejected (if rejected)
);

/// <summary>
/// Represents a category of units (Length, Weight, Temperature, etc.).
/// </summary>
public sealed record UnitCategory(string Name);

/// <summary>
/// The lifecycle state of a unit submission.
/// </summary>
public enum UnitStatus
{
    Pending,    // Submitted, awaiting approval
    Approved,   // Approved by admin, visible in public API
    Rejected    // Rejected, hidden from public
}

/// <summary>
/// Conversion metadata for a unit (how to convert to/from base unit).
/// </summary>
public sealed record UnitConversion(
    decimal ScaleToBase,      // e.g., 1000 for km (1 km = 1000 meters)
    decimal OffsetToBase = 0  // e.g., 273.15 for Celsius (for affine conversions)
);

/// <summary>
/// Quantity: a value paired with its unit.
/// </summary>
public sealed record Quantity(decimal Value, Unit Unit);

/// <summary>
/// Result of a conversion operation.
/// </summary>
public sealed record ConversionResult(decimal Value, Unit Unit);
```

---

## 2. Conversion Rules (Strategy Pattern)

From: `Conversions.feature` (convert length, weight, temperature; reject incompatible)

```csharp
/// <summary>
/// Defines how to convert between units within a category.
/// Implementations: AffineConversionRule (linear + offset), FormulaConversionRule (custom).
/// </summary>
public interface IConversionRule
{
    /// <summary>
    /// Can this rule convert from one unit to another?
    /// </summary>
    bool CanConvert(Unit from, Unit to);

    /// <summary>
    /// Perform the conversion. Throws if CanConvert is false.
    /// </summary>
    /// <exception cref="IncompatibleUnitsException">Thrown if units are incompatible.</exception>
    decimal Convert(decimal value, Unit from, Unit to);
}
```

---

## 3. Unit Catalog & Queries

From: `Conversions.feature`, `ApprovalWorkflow.feature` (list approved, list all for admin)

```csharp
/// <summary>
/// Read-only access to the unit catalog.
/// Queries are safe to cache and parallelize.
/// </summary>
public interface IUnitQueryRepository
{
    /// <summary>
    /// Get a unit by symbol (e.g., "m", "kg", "celsius").
    /// </summary>
    Task<Unit?> GetBySymbolAsync(string symbol, CancellationToken ct = default);

    /// <summary>
    /// List all approved units (public-facing).
    /// </summary>
    Task<IReadOnlyList<Unit>> ListApprovedAsync(CancellationToken ct = default);

    /// <summary>
    /// List all units in a category that are approved.
    /// </summary>
    Task<IReadOnlyList<Unit>> ListApprovedByCategoryAsync(
        string categoryName, CancellationToken ct = default);

    /// <summary>
    /// List ALL units submitted by a specific user (any status).
    /// Used by employees/partners to see their submissions.
    /// </summary>
    Task<IReadOnlyList<Unit>> ListBySubmitterAsync(
        string submitterId, CancellationToken ct = default);

    /// <summary>
    /// List ALL units in a category (for admin review).
    /// </summary>
    Task<IReadOnlyList<Unit>> ListByCategoryAsync(
        string categoryName, CancellationToken ct = default);

    /// <summary>
    /// List all units with a specific status (Pending, Approved, Rejected).
    /// Admin use case.
    /// </summary>
    Task<IReadOnlyList<Unit>> ListByStatusAsync(
        UnitStatus status, CancellationToken ct = default);

    /// <summary>
    /// Get a single unit by ID.
    /// </summary>
    Task<Unit?> GetByIdAsync(string unitId, CancellationToken ct = default);

    /// <summary>
    /// Get all categories.
    /// </summary>
    Task<IReadOnlyList<UnitCategory>> GetCategoriesAsync(CancellationToken ct = default);
}

/// <summary>
/// Write access to units (persistence, state transitions).
/// </summary>
public interface IUnitCommandRepository
{
    /// <summary>
    /// Submit a new unit (status = Pending).
    /// </summary>
    Task<Unit> CreateAsync(Unit unit, CancellationToken ct = default);

    /// <summary>
    /// Update an existing unit (only if Pending).
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if unit is not Pending.</exception>
    Task UpdateAsync(Unit unit, CancellationToken ct = default);

    /// <summary>
    /// Approve a pending unit (transition Pending → Approved).
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if unit is not Pending.</exception>
    Task ApproveAsync(string unitId, string approvedBy, CancellationToken ct = default);

    /// <summary>
    /// Reject a pending unit (transition Pending → Rejected).
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if unit is not Pending.</exception>
    Task RejectAsync(
        string unitId, string rejectedBy, string reason, CancellationToken ct = default);

    /// <summary>
    /// Persist all changes to the repository.
    /// </summary>
    Task SaveAsync(CancellationToken ct = default);
}
```

---

## 4. Use Cases (Application Layer)

From: All features

```csharp
namespace UnitConverter.Application;

/// <summary>
/// Convert a quantity from one unit to another.
/// </summary>
public sealed record ConvertCommand(decimal Value, string FromUnit, string ToUnit);

public interface IConvertQuantityUseCase
{
    /// <exception cref="Domain.UnknownUnitException">If a unit doesn't exist.</exception>
    /// <exception cref="Domain.IncompatibleUnitsException">If units are in different categories.</exception>
    Task<ConversionResult> HandleAsync(ConvertCommand command, CancellationToken ct = default);
}

/// <summary>
/// List all approved units (public API).
/// </summary>
public interface IListApprovedUnitsUseCase
{
    Task<IReadOnlyList<Unit>> HandleAsync(
        string? categoryName = null, CancellationToken ct = default);
}

/// <summary>
/// List all categories.
/// </summary>
public interface IListCategoriesUseCase
{
    Task<IReadOnlyList<UnitCategory>> HandleAsync(CancellationToken ct = default);
}

/// <summary>
/// Employee/Partner: submit a new unit.
/// </summary>
public sealed record SubmitUnitCommand(
    string Symbol, string Name, string Category, 
    decimal ScaleToBase, decimal OffsetToBase = 0);

public interface ISubmitUnitUseCase
{
    /// <exception cref="Domain.DomainException">If validation fails (duplicate symbol, invalid category, etc.).</exception>
    Task<Unit> HandleAsync(SubmitUnitCommand command, string submittedBy, CancellationToken ct = default);
}

/// <summary>
/// Employee/Partner: edit their own pending submission.
/// </summary>
public sealed record EditUnitCommand(
    string UnitId, string Name, decimal ScaleToBase, decimal OffsetToBase);

public interface IEditUnitUseCase
{
    /// <exception cref="Domain.InvalidOperationException">If unit is not Pending.</exception>
    /// <exception cref="UnauthorizedAccessException">If user is not the submitter.</exception>
    Task HandleAsync(EditUnitCommand command, string userId, CancellationToken ct = default);
}

/// <summary>
/// Employee/Partner: list their own submissions (any status).
/// </summary>
public interface IListMySubmissionsUseCase
{
    Task<IReadOnlyList<Unit>> HandleAsync(string userId, CancellationToken ct = default);
}

/// <summary>
/// Admin: list all pending units for review.
/// </summary>
public interface IListPendingUnitsUseCase
{
    Task<IReadOnlyList<Unit>> HandleAsync(CancellationToken ct = default);
}

/// <summary>
/// Admin: approve a pending unit.
/// </summary>
public sealed record ApproveUnitCommand(string UnitId, string Reason = "");

public interface IApproveUnitUseCase
{
    /// <exception cref="Domain.InvalidOperationException">If unit is not Pending.</exception>
    Task HandleAsync(ApproveUnitCommand command, string approvedBy, CancellationToken ct = default);
}

/// <summary>
/// Admin: reject a pending unit.
/// </summary>
public sealed record RejectUnitCommand(string UnitId, string Reason);

public interface IRejectUnitUseCase
{
    /// <exception cref="Domain.InvalidOperationException">If unit is not Pending.</exception>
    Task HandleAsync(RejectUnitCommand command, string rejectedBy, CancellationToken ct = default);
}
```

---

## 5. Domain Exceptions

From: All features (error scenarios)

```csharp
namespace UnitConverter.Domain;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

public sealed class UnknownUnitException(string symbol)
    : DomainException($"Unknown unit: {symbol}")
{
    public string Symbol => symbol;
}

public sealed class IncompatibleUnitsException(Unit from, Unit to)
    : DomainException(
        $"Cannot convert '{from.Symbol}' ({from.Category.Name}) to "
        + $"'{to.Symbol}' ({to.Category.Name}).")
{
    public Unit From => from;
    public Unit To => to;
}

public sealed class UnitAlreadyExistsException(string symbol)
    : DomainException($"Unit '{symbol}' already exists in this category.")
{
    public string Symbol => symbol;
}

public sealed class InvalidUnitException(string reason)
    : DomainException($"Invalid unit: {reason}")
{
    public string Reason => reason;
}
```

---

## 6. Authorization Ports

From: `Authorization.feature` (who can do what)

```csharp
namespace UnitConverter.Application;

/// <summary>
/// Queries the authorization system to determine what a user can do.
/// </summary>
public interface IAuthorizationPort
{
    /// <summary>
    /// Can the user submit new units?
    /// </summary>
    Task<bool> CanSubmitUnitsAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Can the user approve units (admin only)?
    /// </summary>
    Task<bool> CanApproveUnitsAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Can the user edit a specific unit (owner or admin)?
    /// </summary>
    Task<bool> CanEditUnitAsync(string userId, string unitId, CancellationToken ct = default);
}
```

---

## 7. Partner Management (API Key Authentication)

From: `UnitManagement.feature`, `Authorization.feature` (partner API keys)

```csharp
namespace UnitConverter.Domain;

/// <summary>
/// Represents a partner organization with API key authentication.
/// </summary>
public sealed record Partner(
    string Id,
    string Name,
    string ApiKeyHash,          // PBKDF2/Argon2 hash (never plaintext)
    bool IsActive,
    DateTime CreatedAt,
    DateTime? ExpiresAt = null  // Optional expiration
);

namespace UnitConverter.Application;

/// <summary>
/// Access to partner records (for API key validation).
/// </summary>
public interface IPartnerRepository
{
    /// <summary>
    /// Get a partner by their hashed API key.
    /// </summary>
    Task<Partner?> GetByApiKeyHashAsync(string keyHash, CancellationToken ct = default);

    /// <summary>
    /// Validate an API key (hash and lookup).
    /// </summary>
    Task<(Partner? partner, bool isValid)> ValidateApiKeyAsync(
        string plainTextKey, CancellationToken ct = default);

    /// <summary>
    /// Create a new partner with an API key.
    /// </summary>
    Task<Partner> CreateAsync(string name, string plainTextKey, CancellationToken ct = default);

    /// <summary>
    /// Revoke a partner's API key (set IsActive = false).
    /// </summary>
    Task RevokeAsync(string partnerId, CancellationToken ct = default);

    /// <summary>
    /// Persist changes.
    /// </summary>
    Task SaveAsync(CancellationToken ct = default);
}
```

---

## Summary: Interfaces to Implement

**Domain Layer (framework-free):**
- `IConversionRule` — conversion strategy
- Domain records & exceptions

**Application Layer (orchestration):**
- `IUnitQueryRepository`, `IUnitCommandRepository` — CQRS
- `IConvertQuantityUseCase`, `ISubmitUnitUseCase`, `IApproveUnitUseCase`, etc.
- `IAuthorizationPort`, `IPartnerRepository`

**Infrastructure Layer (persistence):**
- Implement repositories with EF Core
- Seed initial approved units

**API Layer (HTTP):**
- Controllers calling use cases
- Error mapping → `ProblemDetails`
- Bearer token auth for API keys

**Web Layer (Razor Pages):**
- Forms for submission
- Admin dashboard for approval queue
- Both use the same use cases

---

## Next Step

With these interfaces locked in from the requirements, you can now:

1. **Implement Domain Layer** — pure interfaces, value objects, invariants (no framework)
2. **Implement Application Layer** — use cases orchestrating the domain
3. **TDD each use case** — write failing tests, then pass them
4. **Add BDD steps** — Gherkin steps call the use cases
5. **Build API / Web** — thin adapters over the use cases

This is **outside-in TDD**: requirements (Gherkin) → interfaces → implementation → tests all pass together.

