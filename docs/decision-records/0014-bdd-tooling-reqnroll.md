# ADR-0014 — BDD tooling: Reqnroll for .NET (not SpecFlow, not Cucumber)

- Status: Accepted
- Date: 2026-06-03

## Context

We need a BDD framework to execute Gherkin features. Options in the .NET ecosystem:
1. **SpecFlow** — industry standard, but **end-of-life** (last major version 2021; no longer maintained)
2. **Reqnroll** — community-driven fork of SpecFlow, actively maintained, **Gherkin-compatible**
3. **Cucumber.js** or **Behave** — language-agnostic, but require JavaScript or Python
4. **Xbehave.net** — pure .NET, Gherkin-like syntax, minimal tooling

## Decision

Use **Reqnroll** (maintained fork of SpecFlow) in a **dedicated test project** (`tests/UnitConverter.Bdd.Tests/`):

### Why Reqnroll?

| Criterion | SpecFlow | Reqnroll | Cucumber.js | Xbehave |
|-----------|----------|----------|-------------|---------|
| **Maintained** | ❌ EOL | ✅ Active | ✅ Yes | ⚠️ Minimal |
| **Gherkin support** | ✅ Full | ✅ Full | ✅ Full | ⚠️ Custom syntax |
| **.NET integration** | ✅ Native | ✅ Native | ⚠️ Requires Node | ⚠️ Custom |
| **MSTest support** | ✅ Yes | ✅ Yes | ❌ No | ✅ Yes |
| **Maturity** | ✅ Proven | ✅ Proven | ✅ Proven | ❌ Niche |
| **Team familiarity** | ⚠️ Legacy | ✅ New | ❌ JS required | ❌ Custom |

**Winner:** Reqnroll = SpecFlow API + active maintenance + .NET native + MSTest

### Project Structure

```
tests/
├── UnitConverter.Bdd.Tests/          ← Reqnroll features + steps (SEPARATE PROJECT)
│   ├── UnitConverter.Bdd.Tests.csproj
│   ├── Features/
│   │   ├── Conversions.feature
│   │   ├── UnitManagement.feature
│   │   ├── ApprovalWorkflow.feature
│   │   ├── Authorization.feature
│   │   └── Security.feature
│   ├── StepDefinitions/
│   │   ├── ConversionsSteps.cs
│   │   ├── UnitManagementSteps.cs
│   │   ├── ApprovalWorkflowSteps.cs
│   │   ├── AuthorizationSteps.cs
│   │   └── SecuritySteps.cs
│   ├── Hooks/
│   │   └── Hooks.cs                  (setup/teardown)
│   └── appsettings.json              (test config)
├── UnitConverter.Domain.Tests/
├── UnitConverter.Application.Tests/
├── UnitConverter.Api.Tests/
└── UnitConverter.Load.Tests/
```

### Setup (Reqnroll + MSTest)

**UnitConverter.Bdd.Tests.csproj:**
```xml
<ItemGroup>
    <PackageReference Include="Reqnroll" Version="1.0.0" />
    <PackageReference Include="Reqnroll.MsTest" Version="1.0.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="MSTest.TestFramework" />
    <PackageReference Include="MSTest.TestAdapter" />
</ItemGroup>
```

**Step definition example (MSTest runner):**
```csharp
[Binding]
public class ConversionsSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly IConvertQuantityUseCase _useCase;
    
    public ConversionsSteps(ScenarioContext scenarioContext, IConvertQuantityUseCase useCase)
    {
        _scenarioContext = scenarioContext;
        _useCase = useCase;
    }

    [Given(@"the conversion system has approved units for length")]
    public void GivenApprovedLengthUnits()
    {
        // Arrange
    }

    [When(@"I request to convert (\d+) meters to kilometers")]
    public void WhenConvertMetersToKilometers(int value)
    {
        // Act
        var result = _useCase.HandleAsync(
            new ConvertCommand(value, "m", "km")).Result;
        _scenarioContext["result"] = result;
    }

    [Then(@"the result should be (\d+) kilometer")]
    public void ThenResultIsKilometer(int expected)
    {
        // Assert
        var result = (ConversionResult)_scenarioContext["result"];
        Assert.AreEqual(expected, result.Value);
    }
}
```

### Why Separate Project?

- **Decoupling:** BDD tests are **outside-in** (API/UI perspective), not unit-focused
- **Different runtime:** BDD runs against the full DI container + use cases; unit tests run in isolation
- **Parallel execution:** BDD suite can run independently of unit/integration tests
- **Maintainability:** features + step definitions grouped; easy to find scenarios
- **CI/CD:** can run BDD separately (slow) or skip if only unit tests changed (fast feedback)

### Reqnroll vs Alternatives

**Why not SpecFlow?** EOL; no active support.

**Why not Cucumber.js?** Requires Node.js; adds non-.NET tooling to the pipeline; team must know JavaScript.

**Why not Xbehave?** Custom syntax (not Gherkin); smaller community; less mature.

## Rationale

- Reqnroll is the **drop-in replacement** for SpecFlow (same API, same Gherkin syntax)
- Actively maintained by the community (issues fixed, new features)
- Integrates cleanly with MSTest (our unit test framework)
- Gherkin is language-agnostic: features can be shared with non-.NET teams
- Separate project ensures BDD tests are truly outside-in

## Consequences

- Must maintain Reqnroll dependency (monitor for updates)
- Step definitions are new code (must be tested/reviewed like production code)
- BDD suite is slower than unit tests (full DI, DB, etc.)

## Alternatives considered

- **SpecFlow** — rejected; EOL and no longer maintained
- **Cucumber + JavaScript bridge** — rejected; adds complexity, team must know JS
- **Xbehave** — rejected; non-standard syntax, smaller ecosystem
- **No BDD at all** — rejected; requirement is to have living documentation
