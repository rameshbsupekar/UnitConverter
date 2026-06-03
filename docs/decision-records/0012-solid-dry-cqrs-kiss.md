# ADR-0012 — SOLID, DRY, CQRS, and KISS as guiding principles

- Status: Accepted
- Date: 2026-06-03
- **Scope:** Every layer (Domain, Application, Infrastructure, API, Web)

## Context

Professional-quality code requires disciplines beyond architecture. SOLID (extensibility, testability),
DRY (maintainability), CQRS (clarity, scalability), and KISS (sustainability) are non-negotiable
for a codebase you'll own long-term.

## Decision

Apply these principles **rigorously** in code review and test design:

### SOLID Principles

| Principle | Application | Example |
|-----------|-------------|---------|
| **S**ingle Responsibility | One class, one reason to change. | `ConvertQuantityUseCase` handles conversion only; logging/telemetry via interceptors, not inline |
| **O**pen/Closed | Open for extension, closed for modification. | Adding a new unit = data only; no controller/rule changes |
| **L**iskov Substitution | Subtypes must be substitutable for base types. | Any `IConversionRule` impl works interchangeably; no special casing |
| **I**nterface Segregation | Don't force clients to depend on unused methods. | `IUnitCatalog` doesn't include approval methods (belong to `IUnitApprovalRepository`) |
| **D**ependency Inversion | Depend on abstractions, not concrete implementations. | Controllers take `IUnitRepository`, not `SqlServerUnitRepository` |

**Code checkpoint:** In every PR, ask: "Did I introduce an abstraction I can't justify? Could I delete this interface and replace with a simpler type?"

### DRY (Don't Repeat Yourself)

**Rule:** Duplication is **only acceptable** if it's accidental and unrelated (two unrelated business rules that happen to look similar).

- **Duplication in tests?** Extract a shared builder or helper.
- **Duplication in config / migrations?** Consolidate or use code generation.
- **Duplication in domain logic?** Pull into a shared domain service or value object.
- **Duplication in queries?** Use EF Core extensions or a query builder.

**Example (rejected):**
```csharp
// BAD: DRY violation
var approved = units.Where(u => u.Status == UnitStatus.Approved && u.Category == category);
var pending = units.Where(u => u.Status == UnitStatus.Pending && u.Category == category);
```

**Better:**
```csharp
// Apply a shared filter method
var approved = UnitFilter.ByStatusAndCategory(units, UnitStatus.Approved, category);
var pending = UnitFilter.ByStatusAndCategory(units, UnitStatus.Pending, category);
```

**Code checkpoint:** Use `Resharper` or `NDepend` to flag code duplication; resolve or document why it's acceptable.

### CQRS (Command Query Responsibility Segregation)

**Rule:** Separate **reads** from **writes** at the use-case level (not necessarily separate DB tables; that's premature).

- **Queries** (`Get*`, `List*`) — read-only, no side effects, can be cached, optimized for read paths
- **Commands** (`Create*`, `Update*`, `Delete*`) — modify state, return a result, logged as domain events

**Application layer split:**
```
IUnitQueryRepository
  ├─ Task<Unit?> GetBySymbolAsync(string symbol)
  ├─ Task<IEnumerable<Unit>> ListByStatusAsync(UnitStatus status)
  └─ Task<IEnumerable<Unit>> ListApprovedAsync()

IUnitCommandRepository
  ├─ Task CreateAsync(Unit unit)
  ├─ Task UpdateAsync(Unit unit)
  ├─ Task DeleteAsync(string symbol)  // soft delete
  └─ Task SaveAsync()
```

**Use cases stay segregated:**
```
IListUnitsQuery → IUnitQueryRepository (no writes)
IApproveUnitCommand → IUnitCommandRepository (write, return result)
```

**Benefits:**
- Reads are **obviously** safe (cache-friendly, parallelizable)
- Writes are **obviously** logged (auditable, can emit domain events)
- Schema evolution is clearer (read vs write projections)

**Code checkpoint:** Query use cases have no side effects; command use cases return a result type (success/failure). No confusion.

### KISS (Keep It Simple, Stupid)

**Rule:** Simplest design that **currently** meets requirements. Add abstraction only when a **second real force** appears (not speculative).

**Violations (reject these):**
- "Let's build a plugin system in case someone extends it later" — YAGNI (You Aren't Gonna Need It)
- "We might want to swap DBs" — true! But one DbContext + connection string swap is simpler than multi-ORM abstraction
- "Let's add a service bus layer" — until you have async jobs, this is over-engineering
- "Everyone should use the repository pattern everywhere" — repositories belong at DB boundaries, not in Domain

**Acceptable complexity:**
- Repository pattern (one real force: persistence swapping)
- Policy-based authorization (one real force: role changes)
- Interceptors for logging (one real force: telemetry evolves independently)
- CQRS at use-case level (one real force: reads are fundamentally different from writes)

**Code checkpoint:** When proposing a new abstraction, state the **current problem it solves** and the **minimal implementation**. If you can't justify it in two sentences, it's too complex.

## Application to each layer

### Domain
- **SOLID:** Aggregates are single entities with clear invariants; value objects don't know about persistence
- **DRY:** No duplicate validation rules; extract domain services when two aggregates share logic
- **CQRS:** Queries (e.g., `GetApprovedUnits`) are pure; commands (e.g., `ApproveUnit`) modify state
- **KISS:** No event sourcing, no saga patterns, no temporal queries unless required

### Application
- **SOLID:** Use cases are focused (one command, one query); interfaces are segregated (separate query/command repos)
- **DRY:** Shared validators, mappers, and error handling (not duplicated per use case)
- **CQRS:** Obvious split (`IListUnitsQuery` vs `IApproveUnitCommand`)
- **KISS:** No caching layer, no saga orchestration, no distributed transactions

### Infrastructure
- **SOLID:** Repository impl is single-focused (one entity, one contract); DB context doesn't leak
- **DRY:** Shared migration patterns; EF shadow properties for common fields (CreatedAt, ModifiedBy)
- **CQRS:** Query repos use `.AsNoTracking()`; command repos track changes
- **KISS:** One DbContext, one migration folder; swap connection string for different DBs

### API & Web
- **SOLID:** Controllers/endpoints are thin; logic is in use cases
- **DRY:** Shared error mappers, shared authorization checks (policies, not attributes)
- **CQRS:** `GET` endpoints call queries; `POST`/`PUT`/`DELETE` call commands
- **KISS:** No business logic in controllers; no data transformation on the boundary (use DTOs cleanly)

## Enforcement

1. **Code review checklist:**
   - Can I delete this abstraction and still solve the problem?
   - Did I introduce duplication? Where's the shared code?
   - Are queries read-only? Are commands write-only?
   - Is each class single-focused?

2. **Architecture visualization (NDepend, Roslyn analyzers):**
   - Detect cyclic dependencies → SOLID violation
   - Flag code duplication → DRY violation
   - Warn on mixed query/command repos → CQRS violation
   - Highlight overly complex types → KISS violation

3. **PR gate:**
   - SOLID: No structural violations (cycles, leaky abstractions)
   - DRY: Duplication < 5% (measured by tooling)
   - CQRS: Segregation is deliberate (PR description states intent)
   - KISS: Changes are scoped to one domain concept

## Consequences

- **Slower initial design** — must justify every abstraction
- **Stricter code reviews** — principle-driven, not just style
- **Fewer "clever" solutions** — boring, linear code wins
- **Long-term payoff** — less refactoring, easier onboarding, lower bug rate

## Alternatives considered

- **No principles** — code degrades quickly; technical debt compounds
- **Too rigid** — every project is different; apply judgment, not dogma
- **Pick-and-choose** — rejected; they reinforce each other (CQRS enables DRY, SOLID enables testability, KISS enables all)
