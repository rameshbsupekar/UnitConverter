# Code Review Checklist — SOLID, DRY, CQRS, KISS

**Reference:** [ADR-0012](decision-records/0012-solid-dry-cqrs-kiss.md)

Use this checklist for **every PR**. All items must pass before merge.

---

## SOLID Principles

### Single Responsibility (S)
- [ ] Does each class have **one reason to change**?
  - Domain entities: model state + invariants only
  - Use cases: orchestrate one command or query
  - Repositories: handle one entity's persistence
  - Controllers: bind input, call use case, map output
- [ ] If a class serves multiple purposes (e.g., logging + validation + business logic), **split it**.

### Open/Closed (O)
- [ ] Can I add a new unit/category **without modifying existing code**?
  - New unit = data + registration, no controller/rule changes ✅
  - Otherwise = architectural debt
- [ ] Did I add an abstract base class or interface to extend later behavior? **Justify it now.**

### Liskov Substitution (L)
- [ ] Can I swap any implementation of `IConversionRule` for another without breaking clients?
  - Example: if `TemperatureRule` requires special handling not in the interface, **add it to the interface**.
- [ ] No `if (rule is TemperatureRule)` casts in calling code.

### Interface Segregation (I)
- [ ] Did I force a client to implement a method it doesn't use?
  - If so, split the interface.
- [ ] Example: `IUnitRepository` should NOT include `ApproveUnit()` (that's `IUnitApprovalRepository`).

### Dependency Inversion (D)
- [ ] Does the code depend on abstractions (interfaces) or concrete types?
  - ✅ `ConvertQuantityUseCase(IUnitCatalog catalog)`
  - ❌ `ConvertQuantityUseCase(SqlServerUnitCatalog catalog)`
- [ ] Are secrets/config injected or hardcoded? (injected ✅)

---

## DRY (Don't Repeat Yourself)

- [ ] Is there copy-pasted code (same logic, different names)?
  - [ ] Extract to a shared method, builder, or validator.
  - [ ] Document why duplication is acceptable (unrelated business rules).
- [ ] Are test fixtures duplicated across test files?
  - [ ] Move to a shared builder or factory.
- [ ] Are migrations or configs repeated?
  - [ ] Consolidate or use code generation.

**Tool check:** Run `dotnet duplicate-checker` (NDepend) or `ReSharper` code duplication analysis. Target: **< 5% duplication**.

---

## CQRS (Command Query Responsibility Segregation)

- [ ] Is the use case a **read** or a **write**?
  - [ ] **Queries** (Get*, List*): no side effects, returns data, can be cached
  - [ ] **Commands** (Create*, Update*, Approve*): modifies state, returns result/status
- [ ] Are query and command repos segregated?
  - ✅ `IUnitQueryRepository` + `IUnitCommandRepository` (separate concerns)
  - ❌ One `IUnitRepository` with both methods (mixed responsibilities)
- [ ] Do query methods modify state? (they should not)
  - Example: `GetUnits()` should never side-effect; if it does, split or document.
- [ ] Are commands **obviously** logged?
  - Commands go through `IUnitCommandRepository.SaveAsync()`, which can emit domain events or audit logs.

---

## KISS (Keep It Simple, Stupid)

- [ ] Did I introduce a new abstraction? **Justify it in 2 sentences.**
  - "We need to swap DBs" ✅ (repository pattern justified)
  - "We might need to swap DBs in the future" ❌ (speculative; add later if it happens)
  - "Everyone should use the X pattern" ❌ (dogma, not pragmatism)
- [ ] Is there a simpler way to solve this problem?
  - Example: before building a plugin system, just add a new service
- [ ] Did I add code I'm not 100% sure will be used?
  - Delete it. Ship the simplest thing.
- [ ] **Red flags (reject these):**
  - Event sourcing (unless you have a real need for full audit history)
  - Saga patterns (unless you have multi-step distributed workflows)
  - Plugin systems (unless you have external integrators)
  - Temporal queries (unless you track history for compliance)
  - Caching layer (unless profiling shows it's needed)

---

## Architecture & Structural Health

- [ ] Are there **circular dependencies**? (Domain → App → Infra → API is the order)
  - ❌ Domain references API
  - ❌ Infrastructure imports Domain models directly into schema logic
- [ ] Do public types have **clear contracts** (interfaces)?
  - [ ] Can I understand the type's responsibilities without reading the implementation?
- [ ] Are **secrets or config hardcoded**?
  - ❌ API keys, connection strings, feature flags in source
  - ✅ All in `appsettings.json` / environment variables
- [ ] Is the **code depth** reasonable (no 10-level nesting)?
  - Use guard clauses to flatten nesting; aim for 3–4 levels max.

---

## Testing & Verification

- [ ] Is every new behavior covered by a test?
  - [ ] Unit test (domain/application logic)
  - [ ] Integration test (API or repository)
  - [ ] BDD scenario (user-visible feature or security case)
- [ ] Are tests **fast** and **deterministic**?
  - ❌ Tests that depend on time-of-day or random data
  - ❌ Tests that take > 5 seconds (too slow to run often)
- [ ] Do test names **describe behavior** (not implementation)?
  - ✅ `Convert_MetresToKilometres_ReturnsExpectedValue`
  - ❌ `Test1`, `Conversion_Works`

---

## Documentation & Clarity

- [ ] Is the change **documented** (PR description, ADR, inline code)?
  - If the change is non-trivial, is there an ADR update?
- [ ] Are **intent-revealing names** used? (functions, variables, classes)
  - ❌ `ProcessData`, `tmp`, `x`
  - ✅ `ConvertValue`, `temporaryUnitList`, `conversionFactor`
- [ ] Are **comments explaining why, not what**?
  - ❌ `i++; // increment i`
  - ✅ `// Use binary search; linear would be O(n) for large catalogs`

---

## Per-Layer Checklist

### Domain
- [ ] No framework references (no EF Core, no ASP.NET, no `ILogger`)
- [ ] Invariants are enforced (state cannot be invalid)
- [ ] Aggregate roots are single-focused
- [ ] Value objects are immutable

### Application
- [ ] Use cases are focused (one command or query per class)
- [ ] Repositories are interfaces (swappable)
- [ ] DTOs map cleanly from/to domain entities
- [ ] No business logic in DTOs

### Infrastructure
- [ ] DbContext doesn't leak (Domain/Application don't reference it)
- [ ] Migrations are version-controlled
- [ ] Connection strings are config-driven
- [ ] EF queries use `.AsNoTracking()` for read-heavy paths

### API / Web
- [ ] Controllers are thin (delegate to use cases)
- [ ] Authorization is policy-based (not role checks in code)
- [ ] Error responses are consistent (ProblemDetails)
- [ ] All inputs validated at the boundary

---

## Red Flags (Reject These)

- [ ] **Leaky abstractions:** internal details visible at boundaries
- [ ] **God objects:** single class doing too much
- [ ] **Silent failures:** exceptions swallowed without logging
- [ ] **Cargo-cult code:** patterns applied without understanding
- [ ] **Dead code:** unreachable or unused methods
- [ ] **Mixed concerns:** infrastructure knowledge in domain, business logic in controller
- [ ] **Temporal coupling:** one method must be called before another (document it in the contract)
- [ ] **Premature optimization:** complex code for hypothetical performance gains

---

## Sign-Off

**Reviewer sign-off:**
- [ ] All SOLID checks pass
- [ ] No DRY violations (< 5% duplication)
- [ ] CQRS segregation is clear
- [ ] No unnecessary complexity (KISS)
- [ ] Code is testable, maintainable, extensible

**Approve** if all boxes are checked. Otherwise, **request changes** with specific references to this checklist.

