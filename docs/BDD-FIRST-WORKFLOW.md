# BDD-First Development Workflow with Reqnroll

**Status:** Ready to implement · **Tool:** Reqnroll (maintained SpecFlow fork) in separate `tests/UnitConverter.Bdd.Tests/` project

---

## The Process

```
1. Write Gherkin features (requirements in business language)
   ↓
2. Extract domain interfaces from the features
   ↓
3. Implement domain layer (pure, framework-free)
   ↓
4. Write BDD step definitions (glue Gherkin to domain/use cases)
   ↓
5. Run Gherkin features; they drive implementation
   ↓
6. Add unit tests (domain logic)
   ↓
7. Build API / Web UI (thin adapters over use cases)
   ↓
8. Add integration tests (HTTP contracts)
   ↓
9. All tests green; features passing; ready to deploy
```

---

## What We've Done (Locked In)

### ✅ Gherkin Features (Requirements)

| Feature | Scenarios | Domain Operations |
|---------|-----------|-------------------|
| `Conversions.feature` | 6 | Convert quantities, reject incompatible, round-trip precision |
| `UnitManagement.feature` | 7 | Submit, edit own (if Pending), see own, owner-based gating |
| `ApprovalWorkflow.feature` | 7 | Admin queue, approve/reject, visibility rules (Pending/Rejected hidden from public) |
| `Authorization.feature` | 7 | Role checks (Public, Employee, Partner, Admin), API key validation |
| `Security.feature` | 6 | Input validation, injection prevention, rate limiting, error handling, correlation IDs |

**Total:** 33 scenarios, 5 feature files, all concrete and testable.

### ✅ Domain Interfaces (From Gherkin)

Extracted and documented in `DOMAIN-INTERFACES-FROM-BDD.md`:
- `IConversionRule` — conversion strategy
- `IUnitQueryRepository`, `IUnitCommandRepository` — CQRS reads/writes
- 8 use case interfaces (`IConvertQuantityUseCase`, `ISubmitUnitUseCase`, `IApproveUnitUseCase`, etc.)
- `IAuthorizationPort`, `IPartnerRepository`
- Domain exceptions & records (all immutable)

---

## How to Implement (Step-by-Step)

### Phase 1: Domain Layer (Pure, Testable)

**M0–M1: Core domain (no framework, no HTTP)**

1. Create `src/UnitConverter.Domain/`:
   - Copy the record/exception types from `DOMAIN-INTERFACES-FROM-BDD.md`
   - Copy `IConversionRule` interface
   - Implement `AffineConversionRule` (handles linear + affine conversions)
   - Add unit tests: "Convert 1000m → 1km", "Convert 0°C → 32°F", "Reject meter→kg"
   - All tests green → Domain is ready

### Phase 2: Application Layer (Use Cases)

**M2–M4: Orchestrate domain**

1. Create `src/UnitConverter.Application/`:
   - Copy use case interfaces from `DOMAIN-INTERFACES-FROM-BDD.md`
   - Implement each use case (e.g., `ConvertQuantityUseCase` calls `IConversionRule`)
   - Depend on `IUnitQueryRepository` (mock in tests)
   - Write unit tests for each use case with Moq
   - Tests don't touch HTTP or DB; they test the domain logic orchestration
   - All tests green → Application layer ready

### Phase 3: BDD Steps (Connect Gherkin to Domain)

**M5: Wire Gherkin features via Reqnroll**

1. Create `tests/UnitConverter.Bdd.Tests/` (separate project):
   - Features: `Conversions.feature`, `UnitManagement.feature`, etc. (already created ✅)
   - `StepDefinitions/` folder with `*.cs` files
   - `Hooks/` for setup/teardown
   - Project file references `Reqnroll`, `Reqnroll.MsTest`, `MSTest.TestFramework`
   - `ConversionsSteps.cs` — Given/When/Then for Conversions.feature
   - `UnitManagementSteps.cs` — for UnitManagement.feature
   - etc.
   
2. Example step:
   ```csharp
   [Given(@"the conversion system has approved units for length")]
   public async Task GivenApprovedLengthUnits()
   {
       // Set up in-memory unit catalog with approved length units
       // Call use case via dependency injection
   }

   [When(@"I request to convert (\d+) meters to kilometers")]
   public async Task WhenConvertMetersToKilometers(int value)
   {
       // Call IConvertQuantityUseCase with the values
       // Store result for Then assertions
   }

   [Then(@"the result should be (\d+) kilometer")]
   public void ThenResultIsKilometers(int expected)
   {
       // Assert result
   }
   ```

3. Run `dotnet test tests/UnitConverter.Bdd.Tests/`
   - Reqnroll executes Gherkin → calls step definitions → calls use cases
   - Tests should pass because domain + application are already tested

### Phase 4: Persistence Layer

**M6–M7: EF Core + repositories**

1. Create `src/UnitConverter.Infrastructure/Data/`:
   - `ApplicationDbContext` (DbSets for Units, Partners, etc.)
   - Migrations
2. Implement `IUnitQueryRepository`, `IUnitCommandRepository`, `IPartnerRepository`
3. Write integration tests: "Create unit in DB, retrieve it, approve it"
4. Seed initial approved units (meter, kilogram, celsius, etc.)
5. All tests still pass ✅

### Phase 5: API Layer

**M8–M9: HTTP endpoints**

1. Create `src/UnitConverter.Api/Controllers/`:
   - `ConversionsController` → `POST /api/conversions` → calls `IConvertQuantityUseCase`
   - `UnitsController` → `GET /api/units`, `POST /api/units` → calls use cases
   - `ApprovalController` → `PUT /api/units/{id}/approve` → calls admin use cases

2. Wire DI in `Program.cs`:
   ```csharp
   services.AddScoped<IConvertQuantityUseCase, ConvertQuantityUseCase>();
   services.AddScoped<IUnitQueryRepository, EfUnitQueryRepository>();
   // ... etc
   ```

3. Add error mapping middleware:
   ```csharp
   app.UseExceptionHandler(handler => handler.Run(async context =>
   {
       // Map domain exceptions → ProblemDetails (404, 422, etc.)
   }));
   ```

4. Add API key authentication (custom middleware):
   ```csharp
   services.AddAuthentication().AddScheme<ApiKeyScheme>(...);
   ```

5. Write API tests via `WebApplicationFactory`:
   - `POST /api/conversions` → 200 with result
   - `POST /api/units` (no auth) → 401
   - `POST /api/units` (employee) → 201
   - `PUT /api/units/{id}/approve` (not admin) → 403
   - All Gherkin scenarios become integration test cases

### Phase 6: Web UI (Razor Pages)

**M10–M11: Admin dashboard + employee forms**

1. Create `src/UnitConverter.Web/`:
   - Pages for submission, approval, public catalog
   - Same use cases (no code duplication)
   - Web components (JS) for interactivity

2. Example: `Pages/Units/Request.cshtml` → calls `ISubmitUnitUseCase`

3. Test via Reqnroll or Playwright (optional, later)

---

## Running the Workflow

### Local Development

```bash
# 1. Run unit tests (domain + application)
dotnet test tests/UnitConverter.Domain.Tests
dotnet test tests/UnitConverter.Application.Tests

# 2. Run BDD features (Reqnroll + MSTest)
dotnet test tests/UnitConverter.Bdd.Tests
# Output: Reqnroll feature report (which scenarios passed/failed)
# Report location: tests/UnitConverter.Bdd.Tests/bin/Debug/net10.0/Reqnroll.TestProjectReport/

# 3. Run API tests (integration)
dotnet test tests/UnitConverter.Api.Tests

# 4. Run the API
dotnet run --project src/UnitConverter.Api
# Visit http://localhost:5000/swagger to see OpenAPI

# 5. Run the Web UI
dotnet run --project src/UnitConverter.Web
# Visit http://localhost:5000 to see dashboard

# 6. Full test suite
dotnet test
# All 33 Gherkin scenarios + unit + integration tests
```

### CI/CD

```yaml
# .github/workflows/build.yml
- Run: dotnet build
- Run: dotnet test
  # Runs all tests (unit, integration, BDD)
  # BDD report is published as artifact
- Run: dotnet publish
  # Builds container image
```

---

## Why This Works (BDD-First)

| Step | Benefit |
|------|---------|
| **Write Gherkin first** | Forces you to think about what the user needs, not implementation details |
| **Extract interfaces** | Decouples layers; easy to mock and test independently |
| **Implement domain** | Pure logic, no framework noise; fast tests, clear invariants |
| **Write BDD steps** | Living documentation; features are always in sync with code |
| **Build API / Web** | Thin adapters; reuse domain + use cases; less code to test |
| **All green** | Requirements (Gherkin) met by passing tests; confidence in deploy |

---

## Current Status (Deliverables)

```
✅ 5 Gherkin feature files (33 scenarios)
   ├─ Conversions.feature (6 scenarios)
   ├─ UnitManagement.feature (7 scenarios)
   ├─ ApprovalWorkflow.feature (7 scenarios)
   ├─ Authorization.feature (7 scenarios)
   └─ Security.feature (6 scenarios)

✅ Domain interfaces (from BDD)
   ├─ IConversionRule
   ├─ IUnitQueryRepository, IUnitCommandRepository (CQRS)
   ├─ 8 use case interfaces
   ├─ IAuthorizationPort, IPartnerRepository
   └─ Domain records & exceptions

✅ Ready to implement step-by-step:
   1. Domain layer (pure, fast tests)
   2. Application layer (use cases)
   3. BDD steps (Gherkin → domain)
   4. Infrastructure (EF Core)
   5. API (HTTP)
   6. Web UI (Razor Pages)
```

---

## Next: Milestone 0 (Scaffolding)

Ready to start Milestone 0?
- Create `src/UnitConverter.Domain/`, `.Application/`, `.Infrastructure/`, `.Web/`
- Create `tests/UnitConverter.Domain.Tests/`, `.Application.Tests/`, `.Bdd.Tests/`, `.Api.Tests/`
- Wire projects into solution
- Commit the Gherkin features + interface stubs

Then Milestone 1: implement the Domain layer (TDD) until all domain tests pass.

